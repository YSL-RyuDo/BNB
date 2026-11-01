"""Production wire parser/intent tests and C#9 syntax check; not an Editor or Unity build."""
import argparse,subprocess
from pathlib import Path

def main(out):
    repo=Path(__file__).resolve().parents[1];out=Path(out).resolve();out.mkdir(parents=True,exist_ok=False)
    for name in ['Assets/Script/Network/WorldProtocol.cs','tools/WorldProtocolTests.cs']:
        p=repo/name;(out/p.name).write_bytes(p.read_bytes())
    sources=out/'syntax';sources.mkdir()
    for source in (repo/'Assets/Script').rglob('*.cs'):
        raw=source.read_bytes()
        try:text=raw.decode('utf-8-sig')
        except UnicodeDecodeError:text=raw.decode('cp949')
        target=sources/source.relative_to(repo/'Assets/Script');target.parent.mkdir(parents=True,exist_ok=True);target.write_text(text,encoding='utf-8')
    (out/'WorldTests.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><NuGetAudit>false</NuGetAudit>
</PropertyGroup><ItemGroup><Compile Remove="syntax/**/*.cs" />
<Reference Include="Microsoft.CodeAnalysis"><HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.dll</HintPath></Reference>
<Reference Include="Microsoft.CodeAnalysis.CSharp"><HintPath>$(MSBuildSDKsPath)/../Roslyn/bincore/Microsoft.CodeAnalysis.CSharp.dll</HintPath></Reference>
</ItemGroup></Project>''')
    p=subprocess.run(['dotnet','run','--project',str(out/'WorldTests.csproj'),'--configuration','Release','--',str(sources)],capture_output=True)
    (out/'results.txt').write_bytes(p.stdout+p.stderr);print(p.stdout.decode(errors='replace'));print(p.stderr.decode(errors='replace'));p.check_returncode()

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--out',required=True);main(p.parse_args().out)
