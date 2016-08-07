using System.Speech.Synthesis;
using UltimateFishBot.Properties;

namespace UltimateFishBot.Classes.BodyParts
{
    class Mouth
    {
        private frmMain m_mainForm;
        T2S t2s = new T2S();

        public Mouth(frmMain mainForm)
        {
            m_mainForm = mainForm;
        }

        public void Say(string text)
        {
            m_mainForm.lblStatus.Text = text;
            if (text == Translate.GetTranslate("manager", "LABEL_PAUSED") || (text == Translate.GetTranslate("manager", "LABEL_STOPPED")))
            {
                m_mainForm.lblStatus.Image = Resources.offline;
            }
            else
            {
                m_mainForm.lblStatus.Image = Resources.online;
            }
            t2s.Say(text);

        }

    }
}
