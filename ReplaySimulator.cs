using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GS_ReplayBrowser
{
    class ReplaySimulator
    {
        public static readonly string[] actorNames = new[] { "黑", "红", "粉", "黄", "蓝" };

        public static string GetDefaultDescription(string filename)
        {
            ReplaySimulator s;

            try
            {
                var rep = new ReplayFile(filename);
                s = new ReplaySimulator(rep);
                s.Simulate();
            }
            catch
            {
                return null;
            }

            var lap = s.SelectedLap;
            var actors = s.SelectedActor;
            var selectedActorNames = actors.Where(a => a != -1).Select(a => actorNames[a]);
            return string.Join("", selectedActorNames) + lap + "周目";
        }

        private static void SetZ(ReplayFile rep, int time, int length = 10)
        {
            for (int i = time; i < time + length; ++i)
            {
                rep.InputData[i * 3 + 0] |= 0x10;
            }
        }

        private readonly ReplayFile _Replay;
        private readonly bool _IsMultiSelect;
        private int _CurrentTime;

        private int _SelectedLap;
        private int[] _SelectedActor;
        private static readonly int[] _DefaultActor = new int[] { 2, 1, 3 };

        private bool _ActorIsFirstSwitch;

        public int SelectedLap
        {
            get
            {
                return _SelectedLap;
            }
        }

        public int[] SelectedActor
        {
            get
            {
                return _SelectedActor.ToArray();
            }
        }

        public string SelectedActorName
        {
            get
            {
                return string.Join("", _SelectedActor.Where(a => a != -1).Select(a => actorNames[a]));
            }
        }

        public ReplaySimulator(ReplayFile rep, bool isMultiSelect = false)
        {
            _Replay = rep;
            _IsMultiSelect = isMultiSelect;
        }

        public void Simulate()
        {
            _CurrentTime = 0;

            Circle();
        }

        private void Circle()
        {
            Skip(1);
            if (!SkipNextZ(60))
            {
                Skip(60);
                Skip(30);
            }
            if (!SkipNextZ(60))
            {
                Skip(60);
            }
            Skip(30);

            _SelectedLap = 1;
            Title();
        }

        private StringBuilder log = new StringBuilder();
        private int logStart = -1;
        private void Log(int val)
        {
            if (logStart == -1)
            {
                logStart = _CurrentTime;
            }
            log.AppendLine("" + (_CurrentTime - logStart) + ": " + val);
        }

        private void Title()
        {
            var index = Array.FindIndex(_Replay.InputData, x => (x & 0x02) != 0);
            Skip(1);

            int lap = _SelectedLap;
            int selector = 0;
            int maxLap = _Replay.BaseLap * _Replay.BaseLap + 1;

            AllInput input = new AllInput();
            while (_CurrentTime * 3 < _Replay.InputData.Length)
            {
                input.P1.Step(_Replay.InputData[_CurrentTime * 3]);
                input.P2.Step(_Replay.InputData[_CurrentTime * 3 + 1]);
                input.P3.Step(_Replay.InputData[_CurrentTime * 3 + 2]);
                var dy = input.Y;
                {
                    var dya = Math.Abs(dy);
                    if (dya != 1 && (dya < 25 || dya % 7 != 0))
                    {
                        dy = 0;
                    }
                }
                if (dy != 0)
                {
                    Log(dy);
                }
                selector += Math.Sign(dy);
                if (selector < 0) selector = 0;
                if (selector > 2) selector = 2;
                if (selector == 1)
                {
                    int x = input.X;
                    if (Math.Abs(x) == 1 || Math.Abs(x) > 13)
                    {
                        var sx = Math.Sign(x);
                        var incr = sx;
                        if (GetB1(_CurrentTime)) incr += sx * 10;
                        if (GetB2(_CurrentTime)) incr += sx * 20;
                        lap += incr;
                        if (lap < 1) lap = 1;
                        if (lap > maxLap) lap = maxLap;
                    }
                }
                if (GetB0(_CurrentTime))
                {
                    if (selector == 2)
                    {
                        //not entering game
                        throw new Exception();
                    }
                    else if (selector == 0)
                    {
                        _SelectedLap = 1;

                        Skip(61);
                        //Skip(60);

                        _ActorIsFirstSwitch = true;
                        Skip(1);

                        //TODO it seems that enter StageSelect_Stage before 265 frame will not
                        //trigger further events
                        if (_CurrentTime < 265)
                        {
                            _CurrentTime = 265;
                        }

                        StageSelect_Stage(new AllInput());
                        return;
                    }
                    else if (selector == 1)
                    {
                        _SelectedLap = lap;

                        Skip(100);

                        _ActorIsFirstSwitch = true;
                        Skip(1);

                        StageSelect_Stage(new AllInput());
                        return;
                    }
                }
                _CurrentTime += 1;
            }
            throw new Exception();
        }

        private void StageSelect_Stage(AllInput input)
        {
            _SelectedActor = new int[] { -1, -1, -1 };
            bool isExit = false;

            while (_CurrentTime * 3 < _Replay.InputData.Length)
            {
                input.P1.Step(_Replay.InputData[_CurrentTime * 3]);
                input.P2.Step(_Replay.InputData[_CurrentTime * 3 + 1]);
                input.P3.Step(_Replay.InputData[_CurrentTime * 3 + 2]);

                if (isExit)
                {
                    if (input.Y < 0)
                    {
                        isExit = false;
                    }
                    else if (GetB0(_CurrentTime))
                    {
                        Skip(60);

                        Title();
                        return;
                    }
                }
                else
                {
                    if (input.Y > 0)
                    {
                        isExit = true;
                    }
                    else
                    {
                        if (GetInputData(_CurrentTime, 0, 0x10))
                        {
                            _SelectedActor[0] = _DefaultActor[0];
                            SwitchActorVisible(input);
                            StageSelect_Actor(input);
                            return;
                        }
                        if (GetInputData(_CurrentTime, 1, 0x10))
                        {
                            _SelectedActor[1] = _DefaultActor[1];
                            SwitchActorVisible(input);
                            StageSelect_Actor(input);
                            return;
                        }
                        if (GetInputData(_CurrentTime, 2, 0x10))
                        {
                            _SelectedActor[2] = _DefaultActor[2];
                            SwitchActorVisible(input);
                            StageSelect_Actor(input);
                            return;
                        }
                    }
                }
                _CurrentTime += 1;
            }
            throw new Exception();
        }

        private void StageSelect_Actor(AllInput input)
        {
            int[] status = new int[] { 0, 0, 0 };
            for (int i = 0; i < 3; ++i) if (_SelectedActor[i] != -1) { status[i] = 1; break; }

            while (_CurrentTime * 3 < _Replay.InputData.Length)
            {
                input.P1.Step(_Replay.InputData[_CurrentTime * 3]);
                input.P2.Step(_Replay.InputData[_CurrentTime * 3 + 1]);
                input.P3.Step(_Replay.InputData[_CurrentTime * 3 + 2]);

                for (int i = 0; i < 3; ++i)
                {
                    if (status[i] == 0)
                    {
                        if (GetB0Pressed(_CurrentTime, i))
                        {
                            _SelectedActor[i] = _DefaultActor[i];

                            for (int j = 0; j < 3; ++j)
                            {
                                if (j != i && status[j] != 0 && _SelectedActor[j] == _SelectedActor[i])
                                {
                                    _SelectedActor[i] = (_SelectedActor[i] + 1) % 5;
                                    j = -1;
                                }
                            }

                            status[i] = 1;
                        }
                    }
                    else if (status[i] == 2)
                    {
                        if (GetB1Pressed(_CurrentTime, i))
                        {
                            status[i] = 1;
                        }
                    }
                    else
                    {
                        //TODO megane check

                        var dx = input.Pn(i).X;
                        var dxa = Math.Abs(dx);
                        if (dxa != 1 && (dxa < 25 || dxa % 7 != 0))
                        {
                            dx = 0;
                        }
                        if (dx != 0)
                        {
                            var dxs = Math.Sign(dx);
                            var pos = _SelectedActor[i] + dxs;
                            while (true)
                            {
                                if (pos < 0 || pos >= 5)
                                {
                                    pos = _SelectedActor[i];
                                    break;
                                }
                                if (IsActorAvailable(status, i, pos))
                                {
                                    break;
                                }
                                pos += dxs;
                            }
                            _SelectedActor[i] = pos;
                        }

                        if (GetB0Pressed(_CurrentTime, i))
                        {
                            status[i] = 2;
                        }
                        else if (GetB1Pressed(_CurrentTime, i))
                        {
                            status[i] = 0;
                        }
                    }
                }

                if (status.Count(s => s == 1) == 0)
                {
                    if (status.Count(s => s == 2) == 0)
                    {
                        SwitchActorVisible(input);
                        StageSelect_Stage(input);
                        return;
                    }
                    else
                    {
                        //finally finished
                        return;
                    }
                }

                _CurrentTime += 1;
            }
            throw new Exception();
        }

        private bool IsActorAvailable(int[] status, int playerID, int actor)
        {
            for (int j = 0; j < 3; ++j)
            {
                if (j != playerID && status[j] != 0 && _SelectedActor[j] == actor)
                {
                    return false;
                }
            }
            return true;
        }

        private void SwitchActorVisible(AllInput input)
        {
            //skip current frame
            _CurrentTime += 1;

            int count = _ActorIsFirstSwitch ? 56 : 57;
            _ActorIsFirstSwitch = false;

            for (int i = 0; i < count; ++i)
            {
                input.P1.Step(_Replay.InputData[_CurrentTime * 3]);
                input.P2.Step(_Replay.InputData[_CurrentTime * 3 + 1]);
                input.P3.Step(_Replay.InputData[_CurrentTime * 3 + 2]);
                _CurrentTime += 1;
            }
        }

        private class AllInput
        {
            public DirectionInputGroup P1 = new DirectionInputGroup(),
                P2 = new DirectionInputGroup(),
                P3 = new DirectionInputGroup();

            public DirectionInputGroup Pn(int n)
            {
                return n == 0 ? P1 : n == 1 ? P2 : P3;
            }

            public int X
            {
                get
                {
                    //find max abs, if equal, 1P higher than 2P
                    var ret = P1.X;
                    if (Math.Abs(P2.X) > Math.Abs(ret))
                    {
                        ret = P2.X;
                    }
                    if (Math.Abs(P3.X) > Math.Abs(ret))
                    {
                        ret = P3.X;
                    }
                    return ret;
                }
            }

            public int Y
            {
                get
                {
                    var ret = P1.Y;
                    if (Math.Abs(P2.Y) > Math.Abs(ret))
                    {
                        ret = P2.Y;
                    }
                    if (Math.Abs(P3.Y) > Math.Abs(ret))
                    {
                        ret = P3.Y;
                    }
                    return ret;
                }
            }

            public int XSign { get { return Math.Sign(X); } }
            public int YSign { get { return Math.Sign(Y); } }
        }

        private class DirectionInputGroup
        {
            private Direction _X = new Direction(), _Y = new Direction();

            public void Step(uint k)
            {
                _X.Step(k & 0x04, k & 0x08);
                _Y.Step(k & 0x01, k & 0x02);
            }

            public int X { get { return _X.Value; } }
            public int Y { get { return _Y.Value; } }
        }

        private class Direction
        {
            public int Value;

            public void Step(uint nn, uint pp)
            {
                var n = nn != 0;
                var p = pp != 0;

                int v = 0;
                if (n) v = -1;
                else if (p) v = 1;

                if (v == 0)
                {
                    Value = 0;
                }
                else if (Value == 0)
                {
                    Value = v;
                }
                else
                {
                    if (v * Value > 0) Value += v;
                    else if (v * Value < 0) Value = v;
                }
            }
        }

        private void Skip(int time)
        {
            _CurrentTime += time;
        }

        private bool SkipNextZ(int max)
        {
            if (max <= 0)
            {
                max = _Replay.InputData.Length / 3 - _CurrentTime;
            }
            for (int i = _CurrentTime; i < _CurrentTime + max; ++i)
            {
                if (GetB0(i))
                {
                    _CurrentTime = i + 1;
                    return true;
                }
            }
            return false;
        }

        private bool GetB0(int time)
        {
            return GetKey(time, 0x10);
        }

        private bool GetB0Pressed(int time, int player)
        {
            return GetInputData(time, player, 0x10) && !GetInputData(time - 1, player, 0x10);
        }

        private bool GetB1Pressed(int time, int player)
        {
            return GetInputData(time, player, 0x20) && !GetInputData(time - 1, player, 0x20);
        }

        private bool GetB1(int time)
        {
            return GetKey(time, 0x20);
        }

        private bool GetB2(int time)
        {
            return GetKey(time, 0x40);
        }

        private bool GetKey(int time, uint key)
        {
            return GetInputData(time, 0, key) ||
                GetInputData(time, 1, key) ||
                GetInputData(time, 2, key);
        }

        private bool GetInputData(int time, int player, uint key)
        {
            return (_Replay.InputData[time * 3 + player] & key) != 0;
        }
    }
}
