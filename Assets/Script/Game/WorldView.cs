using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// Presentation only. Weapon colliders/scripts cannot originate gameplay results.
public sealed class WorldView : MonoBehaviour
{
    private sealed class Visual { public GameObject Object; public Vector3 Position; public Quaternion Rotation; }
    private readonly Dictionary<ulong, Visual> weapons = new Dictionary<ulong, Visual>();
    private readonly Dictionary<string, Vector3> positions = new Dictionary<string, Vector3>();
    private static WorldView instance;
    public static WorldView Get()
    {
        if (instance == null) instance = new GameObject("ServerWorldView").AddComponent<WorldView>();
        return instance;
    }
    private static Vector3 Position(float[] p) => new Vector3(p[0], p[1], p[2]);
    private static Quaternion Rotation(float[] r) => Quaternion.Euler(r[1], r[0], 0);
    public void Receive(WorldPacket p)
    {
        if (p.Command == "PLAYER_STATE")
        {
            Vector3 pos = new Vector3(p.Position[0], 0, p.Position[1]);
            if (p.Nickname == NetworkConnector.Instance.UserNickname)
            {
                var player = GameObject.Find("Character_" + p.Nickname);
                if (player != null) player.GetComponent<LocalPlayerController>()?.Reconcile(pos);
            }
            else positions[p.Nickname] = pos;
        }
        else if (p.Command == "TILE_REMOVE") MapSystem.Instance?.RemoveServerTile((int)p.Position[0], (int)p.Position[1]);
        else if (p.Command == "WEAPON_REMOVE")
        {
            if (weapons.TryGetValue(p.Id, out Visual old)) { Destroy(old.Object); weapons.Remove(p.Id); }
        }
        else if (p.Command == "WEAPON_STATE")
        {
            if (weapons.TryGetValue(p.Id, out Visual visual)) { visual.Position = Position(p.Position); visual.Rotation = Rotation(p.Rotation); }
        }
        else if (p.Command == "WEAPON_SPAWN")
        {
            if (weapons.ContainsKey(p.Id) || weapons.Count >= 32 || WeaponSystem.Instance == null) return;
            GameObject prefab = WeaponSystem.Instance.GetWeaponPrefab(p.Weapon); if (prefab == null) return;
            GameObject obj = Instantiate(prefab, Position(p.Position), Rotation(p.Rotation));
            obj.name = "ServerWeapon_" + p.Id;
            foreach (var component in obj.GetComponentsInChildren<MonoBehaviour>())
                if (component is Sword || component is Arrow || component is Spell || component is Mace || component is Melody || component is Pitchfork || component is Laser)
                    component.enabled = false;
            foreach (var collider in obj.GetComponentsInChildren<Collider>()) collider.enabled = false;
            foreach (var body in obj.GetComponentsInChildren<Rigidbody>()) body.isKinematic = true;
            if (p.Weapon == 6)
            {
                var line = obj.GetComponent<LineRenderer>();
                if (line != null) { line.useWorldSpace = true; line.SetPosition(0, obj.transform.position); line.SetPosition(1, obj.transform.position + obj.transform.forward * p.Length); }
            }
            obj.GetComponent<AudioSource>()?.Play();
            weapons[p.Id] = new Visual { Object = obj, Position = obj.transform.position, Rotation = obj.transform.rotation };
            var attacker = GameObject.Find("Character_" + p.Nickname);
            if (attacker != null) attacker.GetComponent<Animator>()?.SetTrigger("isAttack");
            if (p.Nickname == NetworkConnector.Instance.UserNickname) WeaponSystem.Instance.StartCooldown(p.Weapon == 6 ? 2f : 1.5f);
        }
    }
    private void Update()
    {
        float blend = 1f - Mathf.Exp(-30f * Time.deltaTime);
        foreach (var visual in weapons.Values) if (visual.Object != null)
        {
            visual.Object.transform.position = Vector3.Lerp(visual.Object.transform.position, visual.Position, blend);
            visual.Object.transform.rotation = Quaternion.Slerp(visual.Object.transform.rotation, visual.Rotation, blend);
        }
        if (GameSystem.Instance == null) return;
        foreach (var entry in positions)
        {
            var player = GameObject.Find("Character_" + entry.Key); if (player == null) continue;
            var current = player.transform.position; var next = Vector3.Lerp(current, new Vector3(entry.Value.x, current.y, entry.Value.z), blend);
            GameSystem.Instance.HandleMoveResult("MOVE_RESULT|" + entry.Key + "," + next.x.ToString("R", CultureInfo.InvariantCulture) + "," + next.z.ToString("R", CultureInfo.InvariantCulture));
        }
    }
    private void OnDestroy() { foreach (var visual in weapons.Values) if (visual.Object != null) Destroy(visual.Object); }
}
