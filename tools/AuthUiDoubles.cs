// Test-only stand-ins. Never copied to Assets or used in a player build.
using System;
using System.Collections.Generic;
namespace UnityEngine {
    public class MonoBehaviour { }
    public class GameObject {
        public static Dictionary<Type,object> Instances=new Dictionary<Type,object>();
        public static T FindObjectOfType<T>() where T:class => Instances.TryGetValue(typeof(T),out var value) ? value as T : null;
        public void SetActive(bool active) { }
    }
    public static class Application { public static void Quit() { } }
    public static class Debug {
        public static List<string> Messages=new List<string>();
        public static void Log(object value) { Messages.Add(value.ToString()); }
        public static void LogError(object value) { Messages.Add(value.ToString()); }
    }
    public static class PlayerPrefs {
        public static Dictionary<string,string> Values=new Dictionary<string,string>();
        public static void SetString(string key,string value) { Values[key]=value; }
        public static void Save() { }
    }
}
namespace UnityEngine.SceneManagement {
    public static class SceneManager {
        public static int Loads; public static string LastScene;
        public static void LoadScene(string scene) { LastScene=scene; Loads++; }
    }
}
namespace UnityEngine.UI {
    public class Button {
        public class ClickEvent { public void AddListener(Action handler) { } }
        public ClickEvent onClick=new ClickEvent();
    }
}
namespace TMPro {
    public class TMP_InputField { public string text=""; }
    public class TextMeshProUGUI { public string text=""; }
}
public interface IMessageHandler { void HandleMessage(string message); }
public class NetworkConnector {
    public static NetworkConnector Instance=new NetworkConnector();
    public System.Net.Sockets.NetworkStream Stream;
    public string UserNickname;
    public List<string> CurrentUserList=new List<string>();
    public void RegisterHandler(string command,IMessageHandler handler) { }
    public void RemoveHandler(string command,IMessageHandler handler) { }
}
public class ButtonSoundManager {
    public static ButtonSoundManager Instance;
    public void PlayClick() { }
}
