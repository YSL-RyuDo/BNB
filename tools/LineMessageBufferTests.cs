using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
class LineMessageBufferTests
{
    static void Check(bool value) { if (!value) throw new Exception("Framing check failed"); }
    static void Main()
    {
        var results = new List<string>();var parser = new LineMessageBuffer();
        Action<string> receive = results.Add;
        byte[] a = Encoding.UTF8.GetBytes("MOVE|n0|1"); parser.Append(a,a.Length,receive);Check(results.Count==0);
        a=Encoding.UTF8.GetBytes(",0\nNEXT|ok\nTAIL");parser.Append(a,a.Length,receive);
        Check(results.Count==2 && results[0]=="MOVE|n0|1,0" && results[1]=="NEXT|ok");
        a=Encoding.UTF8.GetBytes("|done\n");parser.Append(a,a.Length,receive);Check(results.Count==3&&results[2]=="TAIL|done");
        Console.WriteLine("PASS split, coalesced, retained tail, no premature dispatch");
        results.Clear();parser=new LineMessageBuffer();a=Encoding.UTF8.GetBytes("CHAT|한국어 日本語\n");
        for(int i=0;i<a.Length;++i)parser.Append(new byte[]{a[i]},1,receive);
        Check(results.Count==1&&results[0]=="CHAT|한국어 日本語");Console.WriteLine("PASS split UTF-8 at every byte");
        results.Clear();parser=new LineMessageBuffer(4);a=Encoding.UTF8.GetBytes("1234\n");parser.Append(a,a.Length,receive);Check(results[0]=="1234");
        bool rejected=false;try{a=Encoding.UTF8.GetBytes("12345");parser.Append(a,a.Length,receive);}catch(InvalidDataException){rejected=true;}Check(rejected);
        Console.WriteLine("PASS exact limit and oversized message");
        rejected=false;try{parser=new LineMessageBuffer();parser.Append(new byte[]{255,10},2,receive);}catch(DecoderFallbackException){rejected=true;}Check(rejected);
        Console.WriteLine("PASS invalid UTF-8 rejected");
    }
}
