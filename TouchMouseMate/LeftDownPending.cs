using System;

namespace Lextm.TouchMouseMate
{
    public class LeftDownPending : IMouseState
    {
        public void Process(MouseEventFlags flag, StateMachine machine)
        {
            if (flag == MouseEventFlags.RightDown)
            {
                Console.WriteLine("left down p->middle down");
                machine.Current = MiddleDown.Instance;
                if (NativeMethods.Section.MiddleClick)
                {
                    NativeMethods.MouseEvent(MouseEventFlags.MiddleDown);
                }

                machine.Timer.Enabled = false;
            }
            else if (flag == MouseEventFlags.Absolute)
            {
                Console.WriteLine("left down p-> left down");
                machine.Current = LeftDown.Instance;
                if (NativeMethods.Section.TouchOverClick)
                {
                    NativeMethods.MouseEvent(NativeMethods.Section.LeftHandMode ? MouseEventFlags.RightDown : MouseEventFlags.LeftDown);
                }

                machine.Timer.Enabled = false;
            }
        }

        public static LeftDownPending Instance = new LeftDownPending();
    }
}