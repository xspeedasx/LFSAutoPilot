using InSimDotNet;
using InSimDotNet.Out;
using InSimDotNet.Packets;
using MoreLinq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using vGenInterfaceWrap;

namespace LFSAutoPilot2
{
    public partial class Form1 : Form
    {
        InSim insim;
        OutSim outsim;
        vGen joystick;
        int hDev = 0;
        int control = 0;
        Paths paths;
        List<Vector> currentPath;

        int pathIndex = 0;
        bool initPhase = true;
        bool randomSpeed = true;
        int speedmod = 100;
        const byte PATH_CLICKID = 150;
        DateTime lastRandomed = DateTime.Now;

        public Form1()
        {
            InitializeComponent();
            paths = new Paths();
            currentPath = paths.paths[0].Waypoints;
        }

        private void DrawClickables()
        {
            insim.Send(new IS_BTN()
            {
                ReqI = 5,
                L = 80,
                T = 40,
                H = 5,
                W = 40,
                BStyle = ButtonStyles.ISB_CLICK | ButtonStyles.ISB_DARK,
                ClickID = 66,
                Text = "Control mode: " + control,
                UCID = 0
            });

            insim.Send(new IS_BTN()
            {
                ReqI = 5,
                L = 40,
                T = 15,
                H = 5,
                W = 40,
                BStyle = ButtonStyles.ISB_CLICK | ButtonStyles.ISB_DARK,
                ClickID = 67,
                Text = "speedmod: " + speedmod,
                UCID = 0,
                TypeIn = 4,
                Caption = "enter new speed mod"
            });

            var i = 0;
            foreach(var p in paths.paths)
            {
                BTNC((byte)(PATH_CLICKID + i), 140, (byte)(25+i*5), 20, 5, "^7"+p.Name);
                i++;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            joystick = new vGen();
            insim = new InSim();

            insim.Initialized += (o, ec) =>
            {
                insim.Send(new IS_BTN() {
                    ReqI = 5,
                    L = 40,
                    T = 20,
                    H = 5,
                    W = 40,
                    BStyle = ButtonStyles.ISB_CLICK,
                    ClickID = 66,
                    Text = "Control mode: 0",
                    UCID = 0
                });

                foreach (var p in paths.paths)
                {
                    MSL("^3Path: ^7" + p.Name);
                }
                DrawClickables();
            };

            insim.Bind(PacketType.ISP_BTC, (ins, bb) =>
            {
                var bt = (IS_BTC)bb;
                if (bt.ClickID == 66)
                {
                    control = (control + 1) % 2;
                } else if(bt.ClickID == 67)
                {
                    initPhase = true;
                } else if(bt.ClickID >= PATH_CLICKID)
                {
                    var p = paths.paths[bt.ClickID - PATH_CLICKID];
                    currentPath = p.Waypoints;
                    MSL("^7Selected path: ^3" + p.Name);
                    initPhase = true;
                }


                DrawClickables();
            });

            insim.Bind(PacketType.ISP_BTT, (ins, bb) =>
            {
                var bt = (IS_BTT)bb;

                if (bt.ClickID == 67)
                {
                    speedmod = int.Parse(bt.Text);
                    MSL("^3Speedmod set to ^7" + speedmod + "%");
                }

                DrawClickables();
            });

            insim.Initialize(new InSimSettings() {
                IName = "^7Afto^1Piliet",
                Host = "127.0.0.1",
                Port = 29997,
                Interval = 100,
                Prefix = '!',
                Flags = InSimFlags.ISF_LOCAL
            });

            outsim = new OutSim(10000);
            outsim.TimedOut += (a, b) => { insim.Send(new InSimDotNet.Packets.IS_MSL() { Msg = "TimeOut..", ReqI = 10, Sound = MessageSound.SND_MESSAGE }); };
            outsim.PacketReceived += Outsim_PacketReceived;

            outsim.Connect("127.0.0.1", 30000);



            joystick.AcquireDev(1, DevType.vJoy, ref hDev);
            //joystick.SetDevAxis(hDev, 1, (float)100.0);
            //timer = new System.Timers.Timer(100);
            //timer.Elapsed += (o, ee) => {
            //    //joystick.SetDevAxis(hDev, 1, (float)DateTime.Now.Millisecond*0.1f);
            //    var cpos = Cursor.Position;

            //    joystick.SetDevAxis(hDev, 1, cpos.X / 19.2f);
            //    joystick.SetDevAxis(hDev, 2, cpos.Y / 10.8f);
            //};
            //timer.Start();
        }

        private void MSL(string txt)
        {
            insim.Send(new IS_MSL() { Msg = txt, ReqI = 10, Sound = MessageSound.SND_MESSAGE });
        }

        private void Outsim_PacketReceived(object sender, OutSimEventArgs e)
        {
            drawInfo(e);

            var PosX = e.Pos.X / 65535.0;
            var PosY = e.Pos.Y / 65535.0;
            var PosZ = e.Pos.Z / 65535.0;
            var vel = e.Vel.Length() * 3.6f;

            //insim.Send(new InSimDotNet.Packets.IS_MSL() {Msg = $"vel: {vel.ToString("0.0")} pos: {PosX.ToString("0.0")} {PosY.ToString("0.0")} {PosZ.ToString("0.0")}" });

            var cpos = Cursor.Position;
            var mpos = e.Pos.ToLfsFloatV();
            var heading = (float)( e.Heading * 180f / Math.PI);
            var carangle = (float)(270 + (Math.Atan2(e.Vel.Y, e.Vel.X) * 180f / Math.PI))%360;
            carangle = confine360(carangle);

            var drift = heading - carangle;
            drift = confine360(drift);
            if (vel < 20) drift = 0;

            //var closestD = Math.Sqrt(currentPath.Min(x => x.Dist2(mpos)));

            

            //if((DateTime.Now - lastRandomed).TotalSeconds > 300 && pathIndex >= currentPath.Count - 5)
            //{
            //    Random r = new Random();
            //    var p = paths.paths[r.Next(paths.paths.Count)];
            //    currentPath = p.Waypoints;
            //    MSL("^7Random path: ^3" + p.Name);
            //    initPhase = true;
            //    lastRandomed = DateTime.Now;
            //}

            if (initPhase)
            {
                //var closestD = Math.Sqrt(currentPath.Min(x => x.Dist2(mpos)));
                pathIndex = currentPath.IndexOf(currentPath.MinBy(x => x.Dist2(mpos)));
                initPhase = false;
            }

            var target = currentPath[pathIndex];

            var tdist = target.Dist(mpos);
            while (tdist <= 5)
            {
                pathIndex = (pathIndex + 1)%currentPath.Count;
                target = currentPath[pathIndex];
                tdist = target.Dist(mpos);
            }

            var tgan = (270 +( 180f * Math.Atan2(target.Y - mpos.Y, target.X - mpos.X) / Math.PI) )% 360;
            tgan = confine360((float)tgan);

            var diff = (float)(tgan - heading);
            diff = confine360(diff);
            //if (diff < -180) diff += 360;
            //if (diff > 180) diff -= 360;

            var turn = 50 - 50 * (diff / 30f);
            turn = Math.Min(100, Math.Max(0, turn));
            turn += drift * 0.6f;
            turn = Math.Min(110, Math.Max(-10, turn));
            turn += e.AngVel.Z * 3;
            turn = Math.Min(100, Math.Max(0, turn));
            
            float tspeed = target.Z * speedmod / 100f;

            

            BTN(10, 40, 25, 30, 5, "Closest: ^7" + tdist.ToString("0.000"));
            BTN(11, 40, 30, 30, 5, "^1target: " + tgan.ToString("0.0"));
            BTN(12, 40, 35, 30, 5, "^3diff: " + diff.ToString("0.0"));
            BTN(13, 40, 40, 30, 5, "^7turn: " + turn.ToString("0.0"));
            BTN(14, 40, 45, 30, 5, "^2tspeed: " + tspeed.ToString("0.0"));

            BTN(5, 10, 40, 20, 5, "^7carangle: " + carangle.ToString("0.0"));
            BTN(6, 10, 45, 20, 5, "^7drift: " + drift.ToString("0.0"));

            if (control == 0)
            {
                joystick.SetDevAxis(hDev, 1, cpos.X / 19.2f);
                joystick.SetDevAxis(hDev, 2, 100-cpos.Y / 10.8f);
            }
            else if (control == 1)
            {
                var r = new Random().NextDouble();
                var thr = (tspeed - vel) / 0.2f + (randomSpeed ? (float)r : 0);
                //joystick.SetDevAxis(hDev, 2, vel < 60 ? 0 : 50);
                joystick.SetDevAxis(hDev, 2, 50+thr);
                joystick.SetDevAxis(hDev, 1, (float)turn);
            }
        }

        private float confine360(float val)
        {
            if (val > 180) val -= 360;
            if (val < -180) val += 360;
            return val;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            joystick.RelinquishDev(hDev);
        }

        private void drawInfo(OutSimEventArgs e)
        {
            var mpos = e.Pos.ToLfsFloatV();
            var vel = e.Vel.Length() * 3.6f;
            var headd = e.Heading * 180f / Math.PI;

            BTN(1, 10, 20, 20, 5, "^3x: " + mpos.X.ToString("0.00"));
            BTN(2, 10, 25, 20, 5, "^3y: " + mpos.Y.ToString("0.00"));
            BTN(3, 10, 30, 20, 5, "^2spd: " + vel.ToString("0.00"));
            BTN(4, 10, 35, 20, 5, "^1head: " + headd.ToString("0.0"));
        }

        private void BTN(byte cid, byte l, byte t, byte w, byte h, string txt)
        {
            insim.Send(new IS_BTN()
            {
                ReqI = 6,
                UCID = 0,
                L = l,
                T = t,
                W = w,
                H = h,
                BStyle = ButtonStyles.ISB_DARK,
                Text = txt,
                ClickID = cid
            });
        }

        private void BTNC(byte cid, byte l, byte t, byte w, byte h, string txt)
        {
            insim.Send(new IS_BTN()
            {
                ReqI = 6,
                UCID = 0,
                L = l,
                T = t,
                W = w,
                H = h,
                BStyle = ButtonStyles.ISB_DARK | ButtonStyles.ISB_CLICK,
                Text = txt,
                ClickID = cid
            });
        }
    }
    
    public static class MyExtensions
    {
        public static Vector ToLfsFloatV(this Vec v)
        {
            return new Vector(v.X / 65535f, v.Y / 65535f, v.Z / 65535f);
        }

        public static float Length(this Vector vec)
        {
            return (float)Math.Sqrt(vec.X * vec.X + vec.Y * vec.Y);
        }

        public static float Dist(this Vector v1, Vector v2)
        {
            return (float)Math.Sqrt((v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y));
        }

        public static float Dist2(this Vector v1, Vector v2)
        {
            return (v1.X - v2.X) * (v1.X - v2.X) + (v1.Y - v2.Y) * (v1.Y - v2.Y);
        }
    }
}
