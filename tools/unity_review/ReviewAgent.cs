using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

// Copied into the isolated validation project only. No production source edits.
public sealed class ReviewAgent : MonoBehaviour
{
    [Serializable] class Report {
        public int user, weapon;
        public string map, unityVersion;
        public bool completed;
        public List<string> checks = new List<string>();
        public List<string> errors = new List<string>();
    }
    Report report;
    string folder, room, map;
    int user, weapon;
    bool stopped;
    float deadline;
    static string Arg(string name) => Environment.GetCommandLineArgs().FirstOrDefault(a => a.StartsWith(name + "="))?.Substring(name.Length + 1);
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Boot() {
        if (Arg("--review-user") == null) return;
        var root = new GameObject("ValidationAgent"); DontDestroyOnLoad(root);
        root.AddComponent<ReviewAgent>();
    }
    void Awake() {
        user = Int32.Parse(Arg("--review-user")); weapon = Int32.Parse(Arg("--review-weapon"));
        folder = Arg("--review-output"); map = Arg("--review-map"); room = "review";
        report = new Report { user = user, weapon = weapon, map = map, unityVersion = Application.unityVersion };
        Application.targetFrameRate = 30; Application.runInBackground = true;
        deadline = Time.realtimeSinceStartup + 210;
        Application.logMessageReceived += OnLog;
    }
    void OnLog(string condition, string stack, LogType type) {
        if (type == LogType.Exception || type == LogType.Error || type == LogType.Assert) {
            if (report.errors.Count < 30) report.errors.Add(condition);
            Save();
        }
    }
    void Save() => File.WriteAllText(Path.Combine(folder, "client-" + user + ".json"), JsonUtility.ToJson(report, true));
    void Check(bool ok, string name) {
        if (!ok) throw new InvalidOperationException("Review failed: " + name);
        report.checks.Add(name); Save();
    }
    void Mark(string name) => File.WriteAllText(Path.Combine(folder, name + "-" + user), "ready");
    bool All(string name) => Enumerable.Range(0,4).All(i => File.Exists(Path.Combine(folder,name + "-" + i)));
    IEnumerator Wait(Func<bool> test, string name, float timeout=60) {
        float end=Time.realtimeSinceStartup+timeout;
        while(!test()) { if(Time.realtimeSinceStartup>end)throw new TimeoutException(name); yield return null; }
    }
    static void Invoke(object o, string method) => o.GetType().GetMethod(method, BindingFlags.Instance|BindingFlags.NonPublic).Invoke(o,null);
    static void Direction(LocalPlayerController c, Vector3 d) => typeof(LocalPlayerController).GetField("inputDirection", BindingFlags.Instance|BindingFlags.NonPublic).SetValue(c,d);
    IEnumerator Start() {
        yield return Wait(() => SceneManager.GetActiveScene().name=="LoginScene" && NetworkConnector.Instance?.Stream!=null,"login scene/connection");
        var login=FindObjectOfType<LoginUI>();
        Check(login!=null && login.inputID!=null && login.inputPassword!=null && login.loginButton!=null,"login UI references");
        login.inputID.text="u"+user; login.inputPassword.text="synthetic-only"; login.loginButton.onClick.Invoke();
        yield return Wait(() => SceneManager.GetActiveScene().name=="LobbyScene","login response/scene");
        Check(NetworkConnector.Instance.UserNickname=="n"+user,"real login UI and response parser");
        yield return null;
        var network=NetworkConnector.Instance;
        if(user==0) network.SendWorld("CREATE_ROOM|"+room+"|"+map+"||0\n");
        else {
            yield return Wait(() => File.Exists(Path.Combine(folder,"room-0")),"room creation");
            network.SendWorld("ENTER_ROOM|"+room+"||0\n");
        }
        yield return Wait(() => SceneManager.GetActiveScene().name=="RoomScene" && FindObjectOfType<RoomSender>()!=null,"room scene");
        yield return new WaitForSecondsRealtime(.4f);
        FindObjectOfType<RoomSender>().SendChooseCharacter(room, "n"+user, weapon);
        yield return Wait(() => network.CurrentUserCharacterIndices.TryGetValue("n"+user,out int c) && c==weapon,"character selection");
        Mark("room");
        if(user==0) {
            yield return Wait(() => All("room") && network.CurrentUserList.Count==4,"four-player roster");
            yield return new WaitForSecondsRealtime(.5f);
            var rs=FindObjectOfType<RoomUI>();
            var startButton=rs == null ? null : (UnityEngine.UI.Button)typeof(RoomUI).GetField("startGameButton",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(rs);
            Check(startButton!=null,"start-game UI reference");
            startButton.onClick.Invoke();
        }
        yield return Wait(() => SceneManager.GetActiveScene().name=="GameScene" && network.WorldStarted,"map spawn and readiness",45);
        Check(network.Combat.MatchId!=0,"server match begins after readiness");
        Check(Enumerable.Range(0,4).All(i => GameObject.Find("Character_n"+i)!=null),"four real character prefabs spawned");
        Check(GameObject.Find("Map_"+map)!=null,"requested map instantiated");
        var player=GameObject.Find("Character_n"+user); var control=player.GetComponent<LocalPlayerController>();
        yield return new WaitForSecondsRealtime(.2f);
        Check(control!=null && control.enabled,"local controller initialized");
        // Replace physical keyboard input only; invoke the actual controller's FixedUpdate/send code.
        control.enabled=false; Mark("world"); yield return Wait(() => All("world"),"all clients rendered map");
        if(user==0) {
            var body=player.GetComponent<Rigidbody>(); Vector3 from=body.position, direction=Vector3.zero;
            foreach(Vector3 d in new[]{Vector3.right,Vector3.left,Vector3.forward,Vector3.back}) {
                bool blocked=Physics.SphereCastAll(from+Vector3.up*.8f,.3f,d,.65f).Any(h => !h.collider.isTrigger && !h.transform.IsChildOf(player.transform));
                if(!blocked){direction=d;break;}
            }
            Check(direction!=Vector3.zero,"walkable movement direction found");
            Direction(control,direction);
            for(int i=0;i<10;i++){Invoke(control,"FixedUpdate");yield return new WaitForFixedUpdate();}
            Direction(control,Vector3.zero); Invoke(control,"FixedUpdate");
            yield return new WaitForSecondsRealtime(.5f);
            float moved=Vector3.Distance(from,body.position);
            Check(moved>.15f && moved<1.5f,"real controller input moves through server simulation");
            Vector3 expected=body.position; body.position=expected+Vector3.right*4; player.transform.position=body.position;
            yield return new WaitForSecondsRealtime(.5f);
            Check(Vector3.Distance(expected,body.position)<.5f,"server snapshot corrects local position tampering");
        }
        Mark("movement");yield return Wait(() => All("movement"),"movement completed");
        control.isWeaponMode=true; Invoke(control,"HandleAttackOrBalloonInput");
        yield return Wait(() => WeaponSystem.Instance.isCooldown,"server accepted attack creates visual",5);
        var weapons=FindObjectsOfType<Transform>().Where(t => t.name.StartsWith("ServerWeapon_")).Select(t => t.gameObject).ToArray();
        Check(weapons.Length>0,"server weapon objects instantiated");
        Check(weapons.All(w => w.GetComponentsInChildren<Collider>().All(c=>!c.enabled)),"weapon visuals do not own collision results");
        yield return new WaitForSecondsRealtime(10.5f);
        Check(!FindObjectsOfType<Transform>().Any(t=>t.name.StartsWith("ServerWeapon_")),"server weapon lifetime removes visuals");
        var healthBar=GameObject.Find("UserInfo_n"+user)?.transform.Find("UserHealthBar")?.GetComponent<UnityEngine.UI.Slider>();
        Check(healthBar!=null && healthBar.value>10,"health UI captured after weapon phase");
        int beforeWater=(int)healthBar.value;
        control.isWeaponMode=false; Invoke(control,"HandleAttackOrBalloonInput");
        yield return Wait(() => !BalloonSystem.Instance.CanPlaceBalloon(),"own balloon accepted",5);
        Check(GameObject.FindGameObjectsWithTag("Balloon").Length>0,"balloon visual instantiated");
        Check(!BalloonSystem.Instance.CanPlaceBalloon(),"server balloon event starts UI cooldown");
        yield return Wait(() => network.Combat.TryGetHealth("n"+user,out int hp) && hp<beforeWater,"automatic water damage",8);
        Check(network.Combat.TryGetHealth("n"+user,out int health) && health==beforeWater-10,"silent player receives authoritative water damage");
        Check((int)healthBar.value==health,"health UI reflects server damage");
        yield return Wait(() => GameObject.FindGameObjectsWithTag("Water").Length>0,"water event after damage",2);
        Check(true,"server water cells instantiate water prefabs");
        yield return new WaitForSecondsRealtime(2.5f);
        Check(GameObject.FindGameObjectsWithTag("Balloon").Length==0 && GameObject.FindGameObjectsWithTag("Water").Length==0,"balloon and water visual cleanup");
        var emoticons=EmoticonSystem.Instance;
        Check(emoticons!=null,"emoticon UI initialized");
        typeof(EmoticonSystem).GetMethod("OnEmoticonButtonClicked",BindingFlags.Instance|BindingFlags.NonPublic).Invoke(emoticons,new object[]{0});
        yield return new WaitForSecondsRealtime(.5f);
        var image=GameObject.Find("UserInfo_n"+user)?.transform.Find("UseremoticonImage")?.GetComponent<UnityEngine.UI.Image>();
        Check(image!=null && image.enabled && image.sprite!=null,"emoticon response displayed");
        Mark("finished");yield return Wait(() => All("finished"),"all client assertions");
        report.completed=true; Save(); stopped=true; Application.Quit(report.errors.Count==0?0:1);
    }
    void Update() { if(!stopped && Time.realtimeSinceStartup>deadline){report.errors.Add("Runtime review timeout");Save();stopped=true;Application.Quit(2);} }
    void OnApplicationQuit(){if(report!=null)Save();}
}
