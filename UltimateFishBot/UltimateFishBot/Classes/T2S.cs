using System.Speech.Synthesis;

namespace UltimateFishBot.Classes.BodyParts
{
    class T2S
    {
        SpeechSynthesizer synthesizer = new SpeechSynthesizer();
        bool uset2s;
        string lastMessage;

        public T2S()
        {
            uset2s = Properties.Settings.Default.Txt2speech;
            synthesizer.Volume = 60;  // 0...100
            synthesizer.Rate = 1;     // -10...10
        }

        public void Say(string text)
        {
            //Debug code
            //System.Console.WriteLine("Use T2S: " + uset2s);
            //System.Console.WriteLine("Previous message: " + lastMessage);
            //System.Console.WriteLine("Current message: " + text);
            //System.Console.WriteLine("Synthesizer ready: " + (synthesizer.State == SynthesizerState.Ready));

            // Say asynchronous text through Text 2 Speech synthesizer
            if (uset2s && (lastMessage != text) && (synthesizer.State == SynthesizerState.Ready))
            {
                synthesizer.SpeakAsync(text);
                lastMessage = text;
            }
        }
    }
}
