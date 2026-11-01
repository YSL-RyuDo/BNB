using System;
using System.Globalization;
using System.IO;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

public static class WorldProtocolTests
{
    static int checks;
    static void Check(bool ok,string name) { if(!ok)throw new Exception(name);Console.WriteLine("PASS "+name);checks++; }
    public static void Main(string[] args)
    {
        CultureInfo.CurrentCulture = new CultureInfo("fr-FR");
        Check(WorldProtocol.Move(1,2,.6f,.8f)=="MOVE_INPUT|1|2|0.6,0.8\n", "culture-independent direction packet");
        Check(WorldProtocol.Attack(1,3,90.5f)=="ATTACK|1|3|90.5\n", "attack carries intent and no victim or origin");
        Check(WorldProtocol.Move(1,1,1,1)==null && WorldProtocol.Move(1,1,float.NaN,0)==null &&
            WorldProtocol.Attack(1,1,float.PositiveInfinity)==null && WorldProtocol.Attack(0,1,0)==null, "invalid local inputs rejected");
        Check(WorldProtocol.Parse("PLAYER_STATE|1|alpha|2.5,3.25|3",1,out var p) && p.Position[0]==2.5f && p.Sequence==3, "server position parsed independently of locale");
        Check(WorldProtocol.Parse("WEAPON_SPAWN|1|9|alpha|6|2,0.5,3|90,0|12.5",1,out p) && p.Id==9 && p.Weapon==6 && p.Length==12.5f, "registered weapon spawn");
        Check(WorldProtocol.Parse("WEAPON_STATE|1|9|2,0.5,3|120,-45",1,out p) && p.Rotation[1]==-45, "weapon pose");
        Check(WorldProtocol.Parse("WEAPON_REMOVE|1|9",1,out p) && WorldProtocol.Parse("TILE_REMOVE|1|4,5",1,out p) && WorldProtocol.Parse("COMBAT_BEGIN|1",1,out p), "removal and readiness events");
        foreach(string bad in new[]{"PLAYER_STATE|2|a|1,1|1","PLAYER_STATE|1|a|NaN,1|1","PLAYER_STATE|1|a|1,Infinity|1",
            "WEAPON_SPAWN|1|0|a|0|1,1,1|0,0|0","WEAPON_SPAWN|1|1|a|7|1,1,1|0,0|0",
            "WEAPON_SPAWN|1|1|a|6|1,1,1|0,0|9999","WEAPON_REMOVE|1|1|extra","TILE_REMOVE|1|-1,1",
            "TILE_REMOVE|1|1.5,1","PLAYER_STATE|1|a|1,2,3|1","WEAPON_STATE|1|1|1,1,1|NaN,0",null})
            if(WorldProtocol.Parse(bad,1,out _))throw new Exception("Malformed packet accepted");
        Check(true,"12 malformed or stale events rejected");
        int scripts=0;
        foreach(string file in Directory.GetFiles(args[0],"*.cs",SearchOption.AllDirectories)) {
            var tree=CSharpSyntaxTree.ParseText(File.ReadAllText(file),new CSharpParseOptions(LanguageVersion.CSharp9));
            foreach(var diagnostic in tree.GetDiagnostics())if(diagnostic.Severity==DiagnosticSeverity.Error)throw new Exception(Path.GetFileName(file)+": "+diagnostic);
            scripts++;
        }
        Check(scripts>0,$"C# 9 syntax of {scripts} production scripts (not Unity type checking)");
        Console.WriteLine($"{checks} groups passed");
    }
}
