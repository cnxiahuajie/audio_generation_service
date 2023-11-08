// See https://aka.ms/new-console-template for more information
using System.Reflection;
using System.Speech.Synthesis;
using System.Text;
using AudioGenerationService;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

var log = Logger.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

var factory = new ConnectionFactory()
{
    HostName = "192.168.56.120",
    UserName = "rabbitmq",
    Password = "1234"
};

var exchange = "biz.audio_generation_server";
var queueName = "synthesized_speech";
var wordRoutingKey = "synthesized_speech.word";
var essayRoutingKey = "synthesized_speech.essay";

var minIOUtil = new MinIOUtil("192.168.56.120", "albert", "1234567890");
Dictionary<string, string> voiceNameMap = new();

var ChooseSpeech = () =>
{
    while (true)
    {
        try
        {
            Console.WriteLine("请选择讲述人");
            var sy = new SpeechSynthesizer();
            List<InstalledVoice> installedVoices = sy.GetInstalledVoices().ToList();
            for (int i = 0, len = installedVoices.Count; i < len; i++)
            {
                if (installedVoices[i].VoiceInfo.Gender.Equals(VoiceGender.Male))
                {
                    Console.WriteLine("({0}): {1}", i, installedVoices[i].VoiceInfo.Name);
                }
            }
            Console.Write("请选择男声：");
            var male = Console.ReadLine().ToString().Trim();
            for (int i = 0, len = installedVoices.Count; i < len; i++)
            {
                if (installedVoices[i].VoiceInfo.Gender.Equals(VoiceGender.Female))
                {
                    Console.WriteLine("({0}): {1}", i, installedVoices[i].VoiceInfo.Name);
                }
            }
            Console.Write("请选择女声：");
            var female = Console.ReadLine().ToString().Trim();
            voiceNameMap.Add("Male", installedVoices[int.Parse(male)].VoiceInfo.Name);
            voiceNameMap.Add("Female", installedVoices[int.Parse(female)].VoiceInfo.Name);
            break;
        }
        catch
        {
            log.error("你输入了非法字符。");
        }
    }
};

ChooseSpeech();

using (var connection = factory.CreateConnection())
{
    using var channel = connection.CreateModel();
    #region EventingBasicConsumer
    //定义一个EventingBasicConsumer消费者                                    
    var consumer = new EventingBasicConsumer(channel);
    //接收到消息时触发的事件
    consumer.Received += (model, ea) =>
    {
        var wordHandler = new WordHandler(voiceNameMap, Encoding.UTF8.GetString(ea.Body.ToArray()), minIOUtil, channel);
        if (null != wordHandler)
        {
            void callback(object? args)
            {
                wordHandler.Handle(args);
                log.info("单词处理已完成。");
            }
            ThreadPool.QueueUserWorkItem(callback);
        }
    };
    channel.ExchangeDeclare(exchange, ExchangeType.Topic);
    channel.QueueDeclare(queueName, false, false, false, null);
    channel.QueueBind(queueName, exchange, wordRoutingKey, null);
    channel.QueueBind(queueName, exchange, essayRoutingKey, null);
    //调用消费方法 queue指定消费的队列，autoAck指定是否自动确认，consumer就是消费者对象
    channel.BasicConsume(queue: queueName,
                           autoAck: true,
                           consumer: consumer);
    log.info("准备就绪。");
    Console.ReadKey();
    #endregion
}
