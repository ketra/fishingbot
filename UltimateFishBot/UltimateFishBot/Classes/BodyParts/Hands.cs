﻿using System.Threading;
using System.Windows.Forms;
using UltimateFishBot.Classes.Helpers;
using System;

namespace UltimateFishBot.Classes.BodyParts
{
    class Hands
    {
        private Cursor m_cursor;
        private int m_baitIndex;
        private string[] m_baitKeys;

        public Hands()
        {
            m_baitIndex = 0;
            m_cursor = new Cursor(Cursor.Current.Handle);
            UpdateKeys();
        }

        public void UpdateKeys()
        {
            m_baitKeys = new string[7]
            {
                Properties.Settings.Default.BaitKey1,
                Properties.Settings.Default.BaitKey2,
                Properties.Settings.Default.BaitKey3,
                Properties.Settings.Default.BaitKey4,
                Properties.Settings.Default.BaitKey5,
                Properties.Settings.Default.BaitKey6,
                Properties.Settings.Default.BaitKey7,
            };
        }

        public static void Cast()
        {
            Win32.ActivateWow();
            Thread.Sleep(Properties.Settings.Default.CastingDelay);
            Win32.SendKey(Properties.Settings.Default.FishKey);
        }

        public static void Loot()
        {
            Random rand = new Random();
            Win32.SendMouseClick();
            Thread.Sleep(Properties.Settings.Default.LootingDelay - rand.Next(100,2000) + rand.Next(100,2000));
        }

        public void ResetBaitIndex()
        {
            m_baitIndex = 0;
        }

        public void DoAction(Manager.NeededAction action, Mouth mouth)
        {
            string actionKey = "";
            int sleepTime = 0;

            switch (action)
            {
                case Manager.NeededAction.HearthStone:
                    {
                        actionKey = Properties.Settings.Default.HearthKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_HEARTHSTONE"));
                        sleepTime = 0;
                        break;
                    }
                case Manager.NeededAction.Lure:
                    {
                        actionKey = Properties.Settings.Default.LureKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_LURE"));
                        sleepTime = 3;
                        break;
                    }
                case Manager.NeededAction.Charm:
                    {
                        actionKey = Properties.Settings.Default.CharmKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_CHARM"));
                        sleepTime = 3;
                        break;
                    }
                case Manager.NeededAction.Raft:
                    {
                        actionKey = Properties.Settings.Default.RaftKey;
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_RAFT"));
                        sleepTime = 6;
                        break;
                    }
                case Manager.NeededAction.Bait:
                    {
                        int baitIndex = 0;

                        if (Properties.Settings.Default.CycleThroughBaitList)
                        {
                            if (m_baitIndex >= 6)
                                m_baitIndex = 0;

                            baitIndex = m_baitIndex++;
                        }

                        actionKey = m_baitKeys[baitIndex];
                        mouth.Say(Translate.GetTranslate("manager", "LABEL_APPLY_BAIT", baitIndex));
                        sleepTime = 3;
                        break;
                    }
                default:
                    return;
            }
            Random rand = new Random();
            Win32.ActivateWow();
            Win32.SendKey(actionKey);
            Thread.Sleep(sleepTime * 1000 + rand.Next(20,500));
        }
    }
}
