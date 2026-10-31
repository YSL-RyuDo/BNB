"""Compile actual authentication scripts against UI doubles in an isolated .NET project.
Requires Python 3 and .NET 9 SDK. Does not open/upgrade the Unity project.
"""
import argparse, subprocess
from pathlib import Path

def main(out):
    repo=Path(__file__).resolve().parents[1];out=Path(out).resolve()
    out.mkdir(parents=True,exist_ok=False)
    files=list((repo/'Assets/Script/Login').glob('*.cs'))+[
        repo/'Assets/Script/Network/LineMessageBuffer.cs',repo/'tools/LineMessageBufferTests.cs',
        repo/'tools/AuthTests.cs',repo/'tools/AuthUiDoubles.cs']
    for source in files:
        raw=source.read_bytes()
        try: text=raw.decode('utf-8-sig')
        except UnicodeDecodeError: text=raw.decode('cp949')
        (out/source.name).write_text(text,encoding='utf-8')
    (out/'AuthTests.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework>
  <StartupObject>AuthTests</StartupObject><EnableDefaultCompileItems>true</EnableDefaultCompileItems>
  <NuGetAudit>false</NuGetAudit></PropertyGroup>
</Project>''')
    result=subprocess.run(['dotnet','run','--project',str(out/'AuthTests.csproj'),'--configuration','Release'],capture_output=True)
    (out/'results.txt').write_bytes(result.stdout+result.stderr)
    print(result.stdout.decode('utf-8',errors='replace'))
    result.check_returncode()

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--out',required=True);main(p.parse_args().out)
