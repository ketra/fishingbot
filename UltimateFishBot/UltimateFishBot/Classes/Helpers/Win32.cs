﻿using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace UltimateFishBot.Classes.Helpers
{
    class Win32
    {
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct Point
        {
            public Int32 x;
            public Int32 y;
        }
        public struct DPI
        {
            public Int32 x;
            public Int32 y;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct CursorInfo
        {
            public Int32 cbSize;
            public Int32 flags;
            public IntPtr hCursor;
            public Point ptScreenPos;
        }
        public enum ModKey
        {
            none = 0,
            ctrl = 1,
            alt = 2,
            shift = 3,
            crtl_alt = 4,
            ctrl_shift = 5,
            alt_shift = 6
        };

        public enum keyState
        {
            KEYDOWN     = 0,
            EXTENDEDKEY = 1,
            KEYUP       = 2
        };

        [DllImport("user32.dll", EntryPoint = "FindWindow")]
        private static extern IntPtr FindWindow(string lpClassName, string lpWindowName);

        [DllImport("user32.dll")]
        private static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr hwnd, ref Rect rectangle);

        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);

        [DllImport("user32.dll")]
        private static extern bool GetCursorInfo(out CursorInfo pci);

        [DllImport("user32.dll")]
        private static extern bool DrawIcon(IntPtr hDC, int X, int Y, IntPtr hIcon);

        [DllImport("user32.dll")]
        private static extern bool keybd_event(byte bVk, byte bScan, uint dwFlags, UIntPtr dwExtraInfo);

        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool SendNotifyMessage(IntPtr hWnd, uint Msg, UIntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll")]
        public static extern bool RegisterHotKey(IntPtr hWnd, int id, int fsModifiers, int vk);

        [DllImport("user32.dll")]
        public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap { VERTRES = 10, DESKTOPVERTRES = 117, }

        private const uint WM_LBUTTONDOWN    = 513;
        private const uint WM_LBUTTONUP      = 514;

        private const uint WM_RBUTTONDOWN    = 516;
        private const uint WM_RBUTTONUP      = 517;

        public static Rectangle GetWowRectangle()
        {
            IntPtr Wow = FindWindow("GxWindowClass", "World Of Warcraft");
            Rect Win32ApiRect = new Rect();
            GetWindowRect(Wow, ref Win32ApiRect);
            Rectangle myRect = new Rectangle() { X = Win32ApiRect.Left, Y = Win32ApiRect.Top, Width = (Win32ApiRect.Right - Win32ApiRect.Left), Height = (Win32ApiRect.Bottom - Win32ApiRect.Top) };
            return myRect;
        }

        public static Bitmap GetCursorIcon(CursorInfo actualCursor, int width = 35, int height = 35)
        {
            Bitmap actualCursorIcon = null;

            try
            {
                actualCursorIcon = new Bitmap(width, height);
                Graphics g = Graphics.FromImage(actualCursorIcon);
                Win32.DrawIcon(g.GetHdc(), 0, 0, actualCursor.hCursor);
                g.ReleaseHdc();
            }
            catch (Exception) { }

            return actualCursorIcon;
        }

        static public void ActivateWow()
        {
            ActivateApp(Properties.Settings.Default.ProcName);
            ActivateApp(Properties.Settings.Default.ProcName + "-64");
            ActivateApp("World Of Warcraft");
        }

        static public void ActivateApp(string processName)
        {
            Process[] p = Process.GetProcessesByName(processName);

            // Activate the first application we find with this name
            if (p.Count() > 0)
                SetForegroundWindow(p[0].MainWindowHandle);
        }

        public static void MoveMouse(int x, int y)
        {
            if (SetCursorPos(x, y))
            {
                LastX = x;
                LastY = y;
            }
        }

        public static CursorInfo GetNoFishCursor()
        {
            Rectangle WoWRect = Win32.GetWowRectangle();
            Win32.MoveMouse((WoWRect.X + 10), (WoWRect.Y + 45));
            LastRectX = WoWRect.X;
            LastRectY = WoWRect.Y;
            Thread.Sleep(15);
            CursorInfo myInfo = new CursorInfo();
            myInfo.cbSize = Marshal.SizeOf(myInfo);
            GetCursorInfo(out myInfo);
            return myInfo;
        }

        public static CursorInfo GetCurrentCursor()
        {
            CursorInfo myInfo = new CursorInfo();
            myInfo.cbSize = Marshal.SizeOf(myInfo);
            GetCursorInfo(out myInfo);
            return myInfo;
        }

        public static void SendKey(string sKeys)
        {
            if (sKeys != " ")
            {
                switch ((ModKey)Properties.Settings.Default.ModifierKey)
                {
                    case ModKey.alt:
                        sKeys = string.Format("%({0})", sKeys); // %(X) : Use the alt key
                        break;
                    case ModKey.ctrl:
                        sKeys = string.Format("^({0})", sKeys);
                        break;
                    case ModKey.shift:
                        sKeys = string.Format("+({0})", sKeys);
                        break;
                    case ModKey.alt_shift:
                        sKeys = string.Format("%+({0})", sKeys);
                        break;
                    case ModKey.crtl_alt:
                        sKeys = string.Format("^%({0})", sKeys);
                        break;
                    case ModKey.ctrl_shift:
                        sKeys = string.Format("^+({0})", sKeys); // %(X) : Use the alt key
                        break;
                    case ModKey.none:
                        sKeys = string.Format("{{{0}}}", sKeys);  // {X} : Avoid UTF-8 errors (é, è, ...)
                        break;

                }
                //if (Properties.Settings.Default.UseAltKey)
                //    sKeys = "^+(" + sKeys + ")"; // %(X) : Use the alt key
                //else
                //    sKeys = "{" + sKeys + "}";  // {X} : Avoid UTF-8 errors (é, è, ...)
            }

            SendKeys.Send(sKeys);
            
        }

        public static void SendMouseClick()
        {
            IntPtr Wow = FindWindow("GxWindowClass", "World Of Warcraft");
            long dWord = MakeDWord((LastX - LastRectX), (LastY - LastRectY));

            if (Properties.Settings.Default.ShiftLoot)
                SendKeyboardAction(16, keyState.KEYDOWN);

            SendNotifyMessage(Wow, WM_RBUTTONDOWN, (UIntPtr)1, (IntPtr)dWord);
            Thread.Sleep(100);
            SendNotifyMessage(Wow, WM_RBUTTONUP, (UIntPtr)1, (IntPtr)dWord);

            if (Properties.Settings.Default.ShiftLoot)
                SendKeyboardAction(16, keyState.KEYUP);
        }

        public static bool SendKeyboardAction(Keys key, keyState state)
        {
            return SendKeyboardAction((byte)key.GetHashCode(), state);
        }

        public static bool SendKeyboardAction(byte key, keyState state)
        {
            return keybd_event(key, 0, (uint)state, (UIntPtr)0);
        }

        private static long MakeDWord(int LoWord, int HiWord)
        {
            return (HiWord << 16) | (LoWord & 0xFFFF);
        }

        public static float GetCurrentDPI()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

            return ScreenScalingFactor; // 1.25 = 125%
        }
        

        static private int LastRectX;
        static private int LastRectY;

        static private int LastX;
        static private int LastY;
    }
}
