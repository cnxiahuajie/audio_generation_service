using System.Reflection;
using System.Speech.Synthesis;
using System.Text;
using log4net;
using log4net.Config;
using Newtonsoft.Json;
using RabbitMQ.Client;
namespace AudioGenerationService
{
    internal class WordHandler
    {
        private static Logger log = Logger.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

        private Dictionary<string, string> VoiceNameMap { get; set; }

        private EventMessage EventMessage { get; set; }

        private WordSynthesizedSpeechDO WordSynthesizedSpeechDO { get; set; }

        private EssaySynthesizedSpeechDO EssaySynthesizedSpeechDO { get; set; }

        private MinIOUtil MinIOUtil { get; set; }

        private IModel Channel { get; set; }

        public WordHandler(Dictionary<string, string> voiceNameMap, string eventMessageJSON, MinIOUtil minIOUtil, IModel channel)
        {
            VoiceNameMap = voiceNameMap;
            EventMessage = JsonConvert.DeserializeObject<EventMessage>(eventMessageJSON);
            var type = EventMessage.Additional.GetValueOrDefault("type");
            if (type.Equals("word"))
            {
                WordSynthesizedSpeechDO = JsonConvert.DeserializeObject<WordSynthesizedSpeechDO>(EventMessage.ObjectJSON);
            }
            else if (type.Equals("essay"))
            {
                EssaySynthesizedSpeechDO = JsonConvert.DeserializeObject<EssaySynthesizedSpeechDO>(EventMessage.ObjectJSON);
            }

            MinIOUtil = minIOUtil;
            Channel = channel;
        }

        public async void Handle(object? state)
        {
            if (WordSynthesizedSpeechDO != null)
            {
                WordSynthesizedSpeechDO.Words.ForEach(async word =>
                {
                    // Male
                    SpeechSynthesizer maleSy = new();
                    maleSy.SelectVoice(VoiceNameMap.GetValueOrDefault("Male"));
                    // 这个是需要保存的路径
                    var fileName = word.Sn + "-male.wav";
                    var filePath = "D:\\test\\" + fileName;
                    maleSy.SetOutputToWaveFile(Path.Combine(filePath));
                    // 输出语音
                    maleSy.Speak(word.Spell);
                    // 保存语音
                    maleSy.SetOutputToDefaultAudioDevice();
                    await MinIOUtil.FileUpload("audio", fileName, "audio/wav", filePath).ConfigureAwait(false);

                    // Female
                    SpeechSynthesizer femaleSy = new();
                    femaleSy.SelectVoice(VoiceNameMap.GetValueOrDefault("Female"));
                    // 这个是需要保存的路径
                    fileName = word.Sn + "-female.wav";
                    filePath = "D:\\test\\" + fileName;
                    femaleSy.SetOutputToWaveFile(Path.Combine(filePath));
                    // 输出语音
                    femaleSy.Speak(word.Spell);
                    // 保存语音
                    femaleSy.SetOutputToDefaultAudioDevice();
                    await MinIOUtil.FileUpload("audio", fileName, "audio/wav", filePath).ConfigureAwait(false);

                    // 发送结果
                    EventMessage.Where = "AGS";
                    EventMessage.When = DateTime.Now.ToString("yyyyMMddHH:mm:ss:ffff");
                    Channel.BasicPublish("biz.english_card_server", "finished_synthesized_speech.done", null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(EventMessage)));
                });
            }
            else if (EssaySynthesizedSpeechDO != null)
            {
                Essay essay = EssaySynthesizedSpeechDO.Essay;
                // Male
                SpeechSynthesizer maleSy = new();
                maleSy.SelectVoice(VoiceNameMap.GetValueOrDefault("Male"));
                // 这个是需要保存的路径
                var fileName = essay.Sn + "-male.wav";
                var filePath = "D:\\test\\" + fileName;
                maleSy.SetOutputToWaveFile(Path.Combine(filePath));
                // 输出语音
                maleSy.Speak(essay.Content);
                // 保存语音
                maleSy.SetOutputToDefaultAudioDevice();
                await MinIOUtil.FileUpload("audio", fileName, "audio/wav", filePath).ConfigureAwait(false);

                // Female
                SpeechSynthesizer femaleSy = new();
                femaleSy.SelectVoice(VoiceNameMap.GetValueOrDefault("Female"));
                // 这个是需要保存的路径
                fileName = essay.Sn + "-female.wav";
                filePath = "D:\\test\\" + fileName;
                femaleSy.SetOutputToWaveFile(Path.Combine(filePath));
                // 输出语音
                femaleSy.Speak(essay.Content);
                // 保存语音
                femaleSy.SetOutputToDefaultAudioDevice();
                await MinIOUtil.FileUpload("audio", fileName, "audio/wav", filePath).ConfigureAwait(false);

                // 发送结果
                EventMessage.Where = "AGS";
                EventMessage.When = DateTime.Now.ToString("yyyyMMddHH:mm:ss:ffff");
                Channel.BasicPublish("biz.english_card_server", "finished_synthesized_speech.done", null, Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(EventMessage)));
            }
            log.info("语音合成已完成。");
        }


    }
}
