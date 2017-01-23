using Microsoft.Bot.Builder.Calling;
using Microsoft.Bot.Builder.Calling.Events;
using Microsoft.Bot.Builder.Calling.ObjectModel.Contracts;
using Microsoft.Bot.Builder.Calling.ObjectModel.Misc;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Web;

namespace CallingBot01
{
    public class SimpleCallingBot : ICallingBot
    {
        public ICallingBotService CallingBotService
        {
            get; private set;
        }

        public SimpleCallingBot(ICallingBotService callingBotService)
        {
            if (callingBotService == null)
                throw new ArgumentNullException(nameof(callingBotService));

            this.CallingBotService = callingBotService;

            CallingBotService.OnIncomingCallReceived += OnIncomingCallReceived;
            CallingBotService.OnPlayPromptCompleted += OnPlayPromptCompleted;
            CallingBotService.OnRecognizeCompleted += OnRecognizeCompleted;
            CallingBotService.OnRecordCompleted += OnRecordCompleted;
            CallingBotService.OnHangupCompleted += OnHangupCompleted;
        }

        private Task OnIncomingCallReceived(IncomingCallEvent incomingCallEvent)
        {
            var id = Guid.NewGuid().ToString();
            incomingCallEvent.ResultingWorkflow.Actions = new List<ActionBase>
                {
                    new Answer { OperationId = id },
                    GetPromptForText("Welcome to simple calling bot!")
                };

            return Task.FromResult(true);
        }

        private Task OnPlayPromptCompleted(PlayPromptOutcomeEvent playPromptOutcomeEvent)
        {
            playPromptOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
            {
                CreateIvrOptions("Press 1 to record your voice, Press 2 to hangup.", 2, false)
            };
            return Task.FromResult(true);
        }

        private Task OnRecognizeCompleted(RecognizeOutcomeEvent recognizeOutcomeEvent)
        {
            switch (recognizeOutcomeEvent.RecognizeOutcome.ChoiceOutcome.ChoiceName)
            {
                case "1":
                    var id = Guid.NewGuid().ToString();

                    var prompt = GetPromptForText("Record your message!");
                    var record = new Record
                    {
                        OperationId = id,
                        PlayPrompt = prompt,
                        MaxDurationInSeconds = 10,
                        InitialSilenceTimeoutInSeconds = 5,
                        MaxSilenceTimeoutInSeconds = 2,
                        PlayBeep = true,
                        StopTones = new List<char> { '#' }
                    };
                    recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase> { record };
                    break;
                case "2":
                    recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        GetPromptForText("Goodbye!"),
                        new Hangup { OperationId = Guid.NewGuid().ToString() }
                    };
                    break;
                default:
                    recognizeOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                    {
                        CreateIvrOptions("Press 1 to record your voice, Press 2 to hangup.", 2, false)
                    };
                    break;
            }
            return Task.FromResult(true);
        }

        private async Task OnRecordCompleted(RecordOutcomeEvent recordOutcomeEvent)
        {
            if (recordOutcomeEvent.RecordOutcome.Outcome == Outcome.Success)
            {
                var record = await recordOutcomeEvent.RecordedContent;
                string path = HttpContext.Current.Server.MapPath($"~/{recordOutcomeEvent.RecordOutcome.Id}.wma");
                using (var writer = new FileStream(path, FileMode.Create))
                {
                    await record.CopyToAsync(writer);
                }
            }

            recordOutcomeEvent.ResultingWorkflow.Actions = new List<ActionBase>
                {
                    CreateIvrOptions("Recorded! Press 1 to record your voice again, Press 2 to hangup.", 2, false)
                };
        }

        private Task OnHangupCompleted(HangupOutcomeEvent hangupOutcomeEvent)
        {
            hangupOutcomeEvent.ResultingWorkflow = null;
            return Task.FromResult(true);
        }

        private static Recognize CreateIvrOptions(string textToBeRead, int numberOfOptions, bool includeBack)
        {
            if (numberOfOptions > 9)
                throw new Exception("too many options specified");

            var id = Guid.NewGuid().ToString();
            var choices = new List<RecognitionOption>();
            for (int i = 1; i <= numberOfOptions; i++)
            {
                choices.Add(new RecognitionOption { Name = Convert.ToString(i), DtmfVariation = (char)('0' + i) });
            }
            if (includeBack)
                choices.Add(new RecognitionOption { Name = "#", DtmfVariation = '#' });
            var recognize = new Recognize
            {
                OperationId = id,
                PlayPrompt = GetPromptForText(textToBeRead),
                BargeInAllowed = true,
                Choices = choices
            };

            return recognize;
        }

        private static PlayPrompt GetPromptForText(string text)
        {
            var prompt = new Prompt { Value = text, Voice = VoiceGender.Male };
            return new PlayPrompt { OperationId = Guid.NewGuid().ToString(), Prompts = new List<Prompt> { prompt } };
        }
    }
}