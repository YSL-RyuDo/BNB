"""Audit and build a disposable Unity copy without importing into the source checkout."""
import argparse
import json
import os
from pathlib import Path
import shutil
import subprocess


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--editor', required=True)
    parser.add_argument('--out', required=True)
    args = parser.parse_args()
    source = Path(__file__).resolve().parents[2]
    output = Path(args.out).resolve()
    if output == source or source in output.parents:
        parser.error('Use a new validation directory outside the source repository')
    output.mkdir(parents=True, exist_ok=False)
    project = output / 'project'
    for folder in ['Assets', 'Packages', 'ProjectSettings']:
        shutil.copytree(source / folder, project / folder)
    automation = project / 'Assets/ReviewAutomation'
    (automation / 'Editor').mkdir(parents=True)
    shutil.copyfile(Path(__file__).with_name('ReviewBuild.cs'), automation / 'Editor/ReviewBuild.cs')
    shutil.copyfile(Path(__file__).with_name('ReviewAgent.cs'), automation / 'ReviewAgent.cs')
    env = os.environ.copy()
    for name, folder in [('TEMP', 'temp'), ('TMP', 'temp'), ('UPM_CACHE_ROOT', 'upm-cache')]:
        target = output / folder
        target.mkdir(exist_ok=True)
        env[name] = str(target)
    env['SLAP_REVIEW_OUTPUT'] = str(output / 'build')
    (output / 'source.json').write_text(json.dumps({
        'commit': subprocess.check_output(['git', '-C', str(source), 'rev-parse', 'HEAD'], text=True).strip(),
        'working_tree': subprocess.check_output(['git', '-C', str(source), 'status', '--porcelain'], text=True),
        'adapters': ['Editor audit and runtime input agent injected in copied project',
                     'Validation-only company/product name isolates PlayerPrefs']
    }, indent=2), encoding='utf-8')
    result = subprocess.run([str(Path(args.editor).resolve()), '-batchmode', '-nographics',
                             '-projectPath', str(project), '-executeMethod', 'ReviewBuild.Run',
                             '-logFile', str(output / 'editor.log')], env=env,
                            creationflags=subprocess.CREATE_NO_WINDOW)
    return result.returncode


if __name__ == '__main__':
    raise SystemExit(main())
