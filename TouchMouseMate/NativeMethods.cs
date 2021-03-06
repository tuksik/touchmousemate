﻿using System.Configuration;
using System.Runtime.InteropServices;
using Lextm.TouchMouseMate.Configuration;
using Microsoft.Research.TouchMouseSensor;

namespace Lextm.TouchMouseMate
{
    public static class NativeMethods
    {
        static NativeMethods()
        {
            Config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.PerUserRoamingAndLocal);
            Section = (TouchMouseMateSection)Config.GetSection("touchMouseMate");
            Machine = new StateMachine();
        }

        public static TouchMouseMateSection Section { get; private set; }

        public static System.Configuration.Configuration Config { get; private set; }

        [DllImport("user32.dll", EntryPoint = "SetCursorPos")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool SetCursorPos(int x, int y);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool GetCursorPos(out MousePoint lpMousePoint);

        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);

        private static TouchZone _leftZone;
        private static TouchZone _rightZone;
        private static readonly int[,] LeftMask = new[,]
        {
        	{1, 1, 1, 1, 1,   1, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{1, 1, 1, 1, 1,   1, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 1, 1, 1, 1,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 1, 1, 1, 1,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	
        	{0, 0, 1, 1, 1,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 1, 1, 1,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 1, 1, 1,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0}
        };

        private static readonly int[,] RightMask = new[,]
        {
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 1,   1, 1, 1, 1, 1},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 1,   1, 1, 1, 1, 1},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   1, 1, 1, 1, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   1, 1, 1, 1, 0},
        	
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   1, 1, 1, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   1, 1, 1, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   1, 1, 1, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0},
        	{0, 0, 0, 0, 0,   0, 0, 0, 0, 0,   0, 0, 0, 0, 0}
        };
        
        internal static bool[,] TouchMap = new bool[15,13];
        internal static int[] PixelFound = {0, 0};

        public static void SetCursorPosition(int x, int y)
        {
            SetCursorPos(x, y);
        }

        public static MousePoint GetCursorPosition()
        {
            MousePoint currentMousePoint;
            var gotPoint = GetCursorPos(out currentMousePoint);
            if (!gotPoint)
            {
                currentMousePoint = new MousePoint(0, 0);
            }

            return currentMousePoint;
        }

        public static void MouseEvent(MouseEventFlags value)
        {
            MousePoint position = GetCursorPosition();
            mouse_event((int)value, position.X, position.Y, 0, 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MousePoint
        {
            public int X;
            public int Y;

            public MousePoint(int x, int y)
            {
                X = x;
                Y = y;
            }
        }

        private static readonly StateMachine Machine;

        /// <summary>
        /// Function receiving callback from mouse.
        /// </summary>
        /// <param name="pTouchMouseStatus">Values indicating status of mouse.</param>
        /// <param name="pabImage">Bytes forming image, 13 rows of 15 columns.</param>
        /// <param name="dwImageSize">Size of image, assumed to always be 195 (13x15).</param>
        internal static void TouchMouseCallbackFunction(ref TOUCHMOUSESTATUS pTouchMouseStatus,
                                                        byte[] pabImage,
                                                        int dwImageSize)
        {
            // Reinizialize Touched Zones
            PixelFound[0] = 0;
            PixelFound[1] = 0;
            // Iterate over rows.
            for (int y = 0; y < pTouchMouseStatus.m_dwImageHeight; y++)
            {
                // Iterate over columns.
                for (int x = 0; x < pTouchMouseStatus.m_dwImageWidth; x++)
                {
                    if (pabImage[pTouchMouseStatus.m_dwImageWidth*y + x] != 0)
                        // analyze captured image for touch gesture.
                    {
                        TouchMap[x, y] = true; // touch detected
                        int pixel = pabImage[pTouchMouseStatus.m_dwImageWidth*y + x]; // touch strength recorded
                        if (pixel == 0)
                        {
                        	continue;
                        }

                        // Get the pixel value at current position.
                        if (LeftMask[y,x] == 1)
                        {
                            // Increment values.
                            _leftZone.Consume(x, y, pixel);                            
                        }
                        else if (RightMask[y,x] == 1)
                        {
                            // Increment values.
                            _rightZone.Consume(x, y, pixel);                            
                        }
                    }
                }
            }
            
            // Calculate and display the center of mass for the touches present.
            _leftZone.AppendTime(pTouchMouseStatus.m_dwTimeDelta);
            _rightZone.AppendTime(pTouchMouseStatus.m_dwTimeDelta);
            _leftZone.ComputeCenter();
            _rightZone.ComputeCenter();

            if (pTouchMouseStatus.m_dwTimeDelta == 0)
            {
                // If the time delta is zero then there has been an 
                // undetermined delta since the last report.
                Log.Info("New touch detected");
                Machine.Idle();
                _leftZone.Reset();
                _rightZone.Reset();
            }

            ApproachThree();

            if (pTouchMouseStatus.m_fDisconnect)
            {
                // The mouse is now disconnected, if we had created objects to track 
                // the mouse they would be destroyed here.
                Log.InfoFormat("\nMouse #{0:X4}: Disconnected\n",
                                  (pTouchMouseStatus.m_dwID & 0xFFFF));
            }
        }

        private static void ApproachThree()
        {
            if (_leftZone.KeyUpDetected)
            {
                Machine.Process(MouseEventFlags.LeftUp);
                _leftZone.Movement = 0F;
            }

            if (_rightZone.KeyUpDetected)
            {
                Machine.Process(MouseEventFlags.RightUp);
                _rightZone.Movement = 0F;
            }

            if (_leftZone.KeyDownDetected)
            {
                Machine.Process(MouseEventFlags.LeftDown);
            }

            if (_rightZone.KeyDownDetected)
            {
                Machine.Process(MouseEventFlags.RightDown);
            }

            if (_leftZone.MoveDetected)
            {
                Log.DebugFormat("left move {0}", _leftZone.Movement);
                if (Section.MoveDetect)
                {
                    Machine.Process(MouseEventFlags.Move);
                }

                _leftZone.Movement = 0F;
            }

            if (_rightZone.MoveDetected)
            {
                Log.DebugFormat("right move {0}", _rightZone.Movement);
                if (Section.MoveDetect)
                {
                    Machine.Process(MouseEventFlags.Move);
                }

                _rightZone.Movement = 0F;
            }

            _leftZone.Prepare();
            _rightZone.Prepare();
        }
    }
}

