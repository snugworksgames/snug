// Snug bot simulator: plays the real Snug.Core rules with human-like bots to estimate run length,
// gates, score and coins per skill level before playtesting.
//
// Run (Mono):
//   mcs -target:library -out:Snug.Core.dll ../unity/Assets/Snug/Scripts/Core/*.cs
//   mcs -out:sim.exe -r:Snug.Core.dll SnugSim.cs && mono sim.exe
// Try a tuning change without editing code (any SnugConfig field):
//   mono sim.exe growRate=150 perfectFit=0.82 maxSpeed=340
//
// Bot model: sees its own size with a reaction lag (160-300 ms), compensates for that lag imperfectly,
// cannot toggle faster than every 90 ms, and misjudges each gate's size by 3.5-10% (Gaussian).
using System; using System.Linq; using System.Collections.Generic; using Snug.Core;
// Bot players with reaction delay and size-judgement noise play the real rules (same loop as SnugGame.TickPlay).
static class Sim {
  class G { public GateSpec s; public float y, gap; public bool done; }
  static double Gauss(Random r){ return Math.Sqrt(-2*Math.Log(1-r.NextDouble()))*Math.Cos(2*Math.PI*r.NextDouble()); }
  static (float sec,int gates,int score,bool crash,int coins) Play(SnugConfig c, int seed, float delay, float noise, bool firstRun){
    var rnd=new Random(seed*7+1); var gen=new GateGenerator(c,seed); var run=new RunState(c,firstRun);
    float LH=780, BY=LH*c.blobHeightFraction, t=0, dt=1/60f; bool hold=true; var blob=new BlobMotion(c); float r=blob.Radius;
    var gs=new List<G>(); Action<float> add=y=>{var s=gen.Next(); gs.Add(new G{s=s,y=y,gap=s.BaseGap});};
    add(BY-c.firstGateDistance);
    var hist=new Queue<float>(); var err=new Dictionary<int,double>(); float lastToggle=-1, minToggle=0.09f; float comp=(float)(0.8+0.2*rnd.NextDouble());
    while(!run.Ended && t<600){
      t+=dt; run.Seconds+=dt;
      // perception: own radius as it was `delay` seconds ago
      hist.Enqueue(r); while(hist.Count>Math.Max(1,(int)(delay/dt))) hist.Dequeue(); float seenR=hist.Peek();
      var next=gs.FirstOrDefault(g=>!g.done);
      if(next!=null){ if(!err.ContainsKey(next.s.Index)) err[next.s.Index]=Gauss(rnd)*noise;
        float target=(float)(next.gap*(0.92+err[next.s.Index])/2); float db=3f;
        // predictive control: estimate where my blob is *now* from what I saw, imperfectly compensating my lag
        float pred=seenR+(hold?1:-1)*blob.Rate(hold)*delay*comp;
        if(t-lastToggle>=minToggle){ bool want=hold; if(target>pred+db) want=true; else if(target<pred-db) want=false;
          if(want!=hold){hold=want;lastToggle=t;} } }
      float assist=1f; if(firstRun&&run.Passed<c.assistGates&&next!=null&&Math.Abs(r-next.gap*0.93f/2)<c.assistBand) assist=c.assistRateFactor;
      blob.Step(hold,dt,assist); r=blob.Radius;
      float v=gen.Speed(run.Passed,run.FirstRun);
      foreach(var g in gs){ g.y+=v*dt; g.gap=g.s.GapAt(t); if(!g.done&&g.y>=BY){g.done=true; run.Apply(2*r,g.gap,g.s.Gold); if(run.Ended)break;} }
      gs.RemoveAll(g=>g.y>LH+60); var top=gs[gs.Count-1]; while(top.y>-80){ add(top.y-gen.Spacing(gen.Count)); top=gs[gs.Count-1]; }
    }
    return (run.Seconds,run.Passed,run.Score,run.EndedByCrash,run.Coins);
  }
  static void Main(string[] args){
    var c=new SnugConfig();
    foreach(var kv in args){ var parts=kv.Split('='); var f=typeof(SnugConfig).GetField(parts[0]); f.SetValue(c, f.FieldType==typeof(int)?(object)int.Parse(parts[1]):(object)float.Parse(parts[1], System.Globalization.CultureInfo.InvariantCulture)); }
    var profiles=new[]{("New player (first run)",0.30f,0.10f,true),("Casual",0.24f,0.075f,false),("Regular",0.20f,0.055f,false),("Skilled",0.16f,0.035f,false)};
    Console.WriteLine("profile | median s | p25-p75 s | median gates | median score | crash % | avg coins/run");
    foreach(var p in profiles){ var name=p.Item1; var d=p.Item2; var n=p.Item3; var first=p.Item4;
      var res=Enumerable.Range(0,2000).Select(i=>Play(c,i,d,n,first)).ToList();
      var s=res.Select(x=>x.sec).OrderBy(x=>x).ToList(); var g=res.Select(x=>x.gates).OrderBy(x=>x).ToList(); var sc=res.Select(x=>x.score).OrderBy(x=>x).ToList();
      Console.WriteLine($"{name} | {s[1000]:0} | {s[500]:0}-{s[1500]:0} | {g[1000]} | {sc[1000]} | {100.0*res.Count(x=>x.crash)/res.Count:0} | {res.Average(x=>x.coins):0.0}");
    }
  }
}
