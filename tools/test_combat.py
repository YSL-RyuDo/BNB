"""Compile the production combat cache using .NET; does not substitute for Unity PlayMode."""
import argparse, subprocess
from pathlib import Path

def main(out):
    repo=Path(__file__).resolve().parents[1];out=Path(out).resolve();out.mkdir(parents=True,exist_ok=False)
    for f in ['Assets/Script/Network/CombatState.cs','tools/CombatTests.cs']:
        source=repo/f;(out/source.name).write_bytes(source.read_bytes())
    (out/'CombatTests.csproj').write_text('''<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup>
<OutputType>Exe</OutputType><TargetFramework>net9.0</TargetFramework><NuGetAudit>false</NuGetAudit>
</PropertyGroup></Project>''')
    p=subprocess.run(['dotnet','run','--project',str(out/'CombatTests.csproj'),'--configuration','Release'],capture_output=True)
    (out/'results.txt').write_bytes(p.stdout+p.stderr);print(p.stdout.decode(errors='replace'));p.check_returncode()

if __name__=='__main__':
    p=argparse.ArgumentParser();p.add_argument('--out',required=True);main(p.parse_args().out)
