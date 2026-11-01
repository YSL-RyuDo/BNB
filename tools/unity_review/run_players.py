"""Run four real Unity players against an isolated loopback server and synthetic accounts."""
import argparse, json, socket, subprocess, sys, time
from pathlib import Path


def main():
    a=argparse.ArgumentParser()
    a.add_argument('--out',required=True)
    a.add_argument('--server-repo',required=True)
    a.add_argument('--server-build',required=True)
    a.add_argument('--player',required=True)
    a.add_argument('--map',choices=['Map1','Map2'],default='Map1')
    a.add_argument('--weapons',default='0,1,2,3')
    a.add_argument('--balloons',default='0,1,2,3')
    args=a.parse_args()
    weapons=list(map(int,args.weapons.split(',')))
    types=list(map(int,args.balloons.split(',')))
    if len(weapons)!=4 or any(w<0 or w>6 for w in weapons):
        a.error('--weapons needs four indices in 0..6')
    if len(types)!=4 or any(b<0 or b>9 for b in types):
        a.error('--balloons needs four indices in 0..9')
    build=Path(args.server_build).resolve()
    manifest=json.loads((build/'build.json').read_text())
    if manifest.get('port')!=9000 or 'Server.cpp: bind loopback instead of all interfaces' not in manifest.get('adapter',[]):
        a.error('Build an isolated loopback server on port 9000 with tools/benchmark/build.py')
    SERVER=Path(args.server_repo).resolve()
    sys.path.insert(0,str(SERVER/'tools/benchmark'))
    from run import fixture
    out=Path(args.out).resolve();out.mkdir(parents=True,exist_ok=False);data=out/'data';fixture(data,4)
    for name in ['Map1.csv','Map2.csv']:(data/name).write_bytes((SERVER/'TeamOFYServer'/name).read_bytes())
    lines=(data/'UserProfile.csv').read_text().splitlines()
    for i in range(4):
        fields=lines[i+1].split(',');fields[-1]=str(types[i]);lines[i+1]=','.join(fields)
    (data/'UserProfile.csv').write_text('\n'.join(lines)+'\n')
    clients=[];handles=[];server=None
    probe=socket.socket();probe.bind(('127.0.0.1',9000));probe.close()
    try:
        log=(out/'server.log').open('wb');handles.append(log)
        server=subprocess.Popen([str(build/'bin/TeamOFYServer.exe')],cwd=data,stdout=log,stderr=subprocess.STDOUT,creationflags=subprocess.CREATE_NO_WINDOW)
        for _ in range(100):
            if server.poll() is not None:raise RuntimeError('Synthetic server failed to start')
            try:
                with socket.create_connection(('127.0.0.1',9000),timeout=1) as sock:
                    sock.settimeout(60);sock.sendall(b'GET_ROOM_LIST|\n');reply=sock.recv(4096)
                    if reply.startswith(b'ROOM_LIST|'):break
            except OSError:time.sleep(.1)
        else:raise TimeoutError('Server readiness')
        player=Path(args.player).resolve()
        for i in range(4):
            cmd=[str(player),'-batchmode','-nographics','-logFile',str(out/f'player-{i}.log'),f'--review-user={i}',f'--review-weapon={weapons[i]}',f'--review-map={args.map}',f'--review-output={out}']
            clients.append(subprocess.Popen(cmd,cwd=out,creationflags=subprocess.CREATE_NO_WINDOW))
            start=time.monotonic()
            while time.monotonic()-start<35:
                p=out/f'client-{i}.json'
                if clients[-1].poll() is not None:break
                if p.exists():
                    try:
                        report=json.loads(p.read_text());checks=report.get('checks',[])
                        if 'real login UI and response parser' in checks or report.get('errors'):break
                    except json.JSONDecodeError:pass
                time.sleep(.2)
            print('Started real Unity client',i,flush=True)
        start=time.monotonic()
        while time.monotonic()-start<240 and any(p.poll() is None for p in clients):time.sleep(1)
        reports=[]
        for i,p in enumerate(clients):
            f=out/f'client-{i}.json';r=json.loads(f.read_text()) if f.exists() else {'errors':['No client report']}
            r['process_exit']=p.poll();reports.append(r)
        (out/'summary.json').write_text(json.dumps(reports,indent=2,ensure_ascii=False),encoding='utf-8')
        passed=all(r.get('completed') and not r.get('errors') and r['process_exit']==0 for r in reports)
        print(json.dumps({'pass':passed,'clients':[{'user':i,'completed':r.get('completed'),'checks':len(r.get('checks',[])),'errors':r.get('errors',[]),'exit':r['process_exit']} for i,r in enumerate(reports)]},ensure_ascii=False),flush=True)
        return 0 if passed else 1
    finally:
        for p in clients+[server]:
            if p is not None and p.poll() is None:p.terminate();p.wait(timeout=15)
        for h in handles:h.close()

if __name__=='__main__':sys.exit(main())
