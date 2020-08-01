//
// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE.md file in the project root for full license information.
//
// carbon_csharp_console.cs: A console application for testing Carbon C# client library.
//

using System;
using System.Diagnostics;

namespace MicrosoftSpeechSDKSamples
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: carbon_csharp_console mode(speech|intent|translation|memleak:cont|single) key(key|token:key) region audioinput(mic|filename|stream:file) model:modelId|lang:language|endpoint:url|devicename:id");
                Environment.Exit(1);
            }

            string subKey = null;
            string region = null;
            string fileName = null;
            string deviceName = null;
            bool useToken = false;
            bool useBaseModel = true;
            bool useEndpoint = false;
            bool isSpeechReco = false;
            bool isIntentReco = false;
            bool isTranslation = false;
            bool isMemoryLeakTest = false;
            string memoryLeakTestKind = null;
            string lang = null;
            string modelId = null;
            string endpoint = null;
            bool useStream = false;
            bool useContinuousRecognition = false;
            bool useOfflineUnidec = false;
            bool useOfflineRnnt = false;

            if (args.Length >= 2)
            {
                var index = args[0].IndexOf(':');
                var modeStr = (index == -1) ? args[0] : args[0].Substring(0, index);
                if (string.Compare(modeStr, "speech", true) == 0)
                {
                    isSpeechReco = true;
                }
                else if (string.Compare(modeStr, "intent", true) == 0)
                {
                    isIntentReco = true;
                }
                else if (string.Compare(modeStr, "translation", true) == 0)
                {
                    isTranslation = true;
                }
                else if (string.Compare(modeStr, "memleak", true) == 0)
                {
                    isMemoryLeakTest = true;
                    if (index != -1)
                    {
                        memoryLeakTestKind = args[0].Substring(index + 1);
                    }
                    index = -1;
                }
                else
                {
                    throw new InvalidOperationException("The specified mode is not supported: " + modeStr);
                }

                if (index != -1)
                {
                    var str = args[0].Substring(index + 1);
                    if (string.Compare(str, "cont", true) == 0)
                    {
                        useContinuousRecognition = true;
                    }
                    else if (string.Compare(str, "single", true) == 0)
                    {
                        useContinuousRecognition = false;
                    }
                    else
                    {
                        throw new ArgumentException("only cont or single is supported.");
                    }
                }
            }

            if (args[1].ToLower().StartsWith("token:"))
            {
                var index = args[1].IndexOf(':');
                if (index == -1)
                {
                    throw new IndexOutOfRangeException("no key is specified.");
                }
                subKey = args[1].Substring(index + 1);
                useToken = true;
            }
            else
            {
                subKey = args[1];
            }

            Trace.Assert(isSpeechReco || isIntentReco || isTranslation || isMemoryLeakTest);
            Trace.Assert(subKey != null);
            if (useToken && (isIntentReco || isTranslation))
            {
                throw new InvalidOperationException("The specified mode is not supported with authorization token: " + args[0]);
            }

            region = args[2];
            if (string.IsNullOrEmpty(region))
            {
                throw new ArgumentException("region may not be empty");
            }

            if (args.Length >= 4)
            {
                var audioInputStr = args[3];

                if (string.Compare(audioInputStr, "mic", true) == 0)
                {
                    fileName = null;
                }
                else if (audioInputStr.ToLower().StartsWith("stream:"))
                {
                    useStream = true;
                    var index = audioInputStr.IndexOf(':');
                    if (index == -1)
                    {
                        throw new IndexOutOfRangeException("No file name specified as stream input.");
                    }
                    fileName = audioInputStr.Substring(index + 1);
                    if (string.IsNullOrEmpty(fileName))
                    {
                        throw new IndexOutOfRangeException("No file name specified as stream input.");
                    }
                }
                else
                {
                    fileName = audioInputStr;
                }
            }

            if (args.Length >= 5)
            {
                var paraStr = args[4];
                if (paraStr.ToLower().StartsWith("lang:"))
                {
                    useBaseModel = true;
                    var index = paraStr.IndexOf(':');
                    if (index == -1)
                    {
                        throw new IndexOutOfRangeException("no language is specified.");
                    }
                    lang = paraStr.Substring(index + 1);
                    if (string.IsNullOrEmpty(lang))
                    {
                        throw new IndexOutOfRangeException("no language is specified.");
                    }
                }
                else if (paraStr.ToLower().StartsWith("model:"))
                {
                    useBaseModel = false;
                    var index = paraStr.IndexOf(':');
                    if (index == -1)
                    {
                        throw new IndexOutOfRangeException("no model is specified.");
                    }
                    modelId = paraStr.Substring(index + 1);
                    if (string.IsNullOrEmpty(modelId))
                    {
                        throw new IndexOutOfRangeException("no model is specified.");
                    }
                }
                else if (paraStr.ToLower().StartsWith("endpoint:"))
                {
                    if (useToken)
                    {
                        throw new InvalidOperationException("Recognition with endpoint is not supported with authorization token.");
                    }

                    useEndpoint = true;
                    var index = paraStr.IndexOf(':');
                    if (index == -1)
                    {
                        throw new IndexOutOfRangeException("no endpoint is specified.");
                    }
                    endpoint = paraStr.Substring(index + 1);
                    if (string.IsNullOrEmpty(endpoint))
                    {
                        throw new IndexOutOfRangeException("no endpoint is specified.");
                    }
                }
                else if (paraStr.ToLower().StartsWith("devicename:"))
                {
                    if (!string.IsNullOrEmpty(fileName) && string.Compare(fileName, "mic", true) != 0)
                    {
                        throw new IndexOutOfRangeException("cannot specify device name when recognizing from file.");
                    }
                    var index = paraStr.IndexOf(':');
                    if (index == -1)
                    {
                        throw new IndexOutOfRangeException("no devicename is specified.");
                    }
                    deviceName = paraStr.Substring(index + 1);
                    if (string.IsNullOrEmpty(deviceName))
                    {
                        throw new IndexOutOfRangeException("no devicename is specified.");
                    }
                }
                else
                {
                    throw new InvalidOperationException("Only the following values are allowed: lang:language, model:modelId, endpoint:url, devicename:id.");
                }
            }

            if (args.Length >= 6)
            {
                if (string.Compare(args[5], "unidec", true) == 0)
                {
                    useOfflineUnidec = true;
                }
                if (string.Compare(args[5], "rnnt", true) == 0)
                {
                    useOfflineRnnt = true;
                }
            }

            if (isMemoryLeakTest)
            {
                MemoryLeakTests.MemoryLeakTest(subKey, region, fileName, memoryLeakTestKind).Wait();
            }
            else if (isSpeechReco)
            {
                if (useEndpoint)
                {
                    Console.WriteLine("=============== Run speech recognition samples by specifying endpoint. ===============");
                    SpeechRecognitionSamples.SpeechRecognitionByEndpointAsync(subKey, endpoint, lang: lang, model: modelId, fileName: fileName, useStream: useStream, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                }
                else
                {
                    if (useOfflineUnidec)
                    {
                        Console.WriteLine("=============== Run speech recognition samples using offline Unidec. ===============");
                        SpeechRecognitionSamples.SpeechRecognitionOfflineUnidecAsync(subKey, region: region, lang: lang, fileName: fileName, useStream: useStream, useToken: useToken, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName).Wait();
                    }
                    else if (useOfflineRnnt)
                    {
                        Console.WriteLine("=============== Run speech recognition samples using offline RNN-T. ===============");
                        SpeechRecognitionSamples.SpeechRecognitionOfflineRnntAsync(subKey, region: region, lang: lang, fileName: fileName, useStream: useStream, useToken: useToken, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName).Wait();
                    }
                    else if (useBaseModel)
                    {
                        Console.WriteLine("=============== Run speech recognition samples using base model. ===============");
                        SpeechRecognitionSamples.SpeechRecognitionBaseModelAsync(subKey, region: region, lang: lang, fileName: fileName, useStream: useStream, useToken: useToken, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName).Wait();
                    }
                    else
                    {
                        Console.WriteLine("=============== Run speech recognition samples using customized model. ===============");
                        SpeechRecognitionSamples.SpeechRecognitionCustomizedModelAsync(subKey, region, modelId, fileName, useStream: useStream, useToken: useToken, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                    }
                }
            }
            else if (isIntentReco)
            {
                if (useEndpoint)
                {
                    Console.WriteLine("=============== Run intent recognition samples by specifying endpoint. ===============");
                    IntentRecognitionSamples.IntentRecognitionByEndpointAsync(subKey, endpoint, fileName, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                }
                else
                {
                    if (useBaseModel)
                    {
                        Console.WriteLine("=============== Run intent recognition samples using base speech model. ===============");
                        IntentRecognitionSamples.IntentRecognitionBaseModelAsync(subKey, region, fileName, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                    }
                    else
                    {
                        Console.WriteLine("=============== Intent recognition with CRIS model is not supported yet. ===============");
                    }
                }
            }
            else if (isTranslation)
            {
                if (useEndpoint)
                {
                    Console.WriteLine("=============== Run translation samples by specifying endpoint. ===============");
                    TranslationSamples.TranslationByEndpointAsync(subKey, endpoint, fileName, useStream: useStream, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                }
                else
                {
                    if (useBaseModel)
                    {
                        Console.WriteLine("=============== Run translation samples using base speech model. ===============");
                        TranslationSamples.TranslationBaseModelAsync(subKey, fileName: fileName, region: region, useStream: useStream, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                    }
                    else
                    {
                        Console.WriteLine("=============== Run translation samples using customized model. ===============");
                        TranslationSamples.TranslationCustomizedModelAsync(subKey, fileName: fileName, region: region, modelId: modelId, useStream: useStream, useContinuousRecognition: useContinuousRecognition, deviceName: deviceName = null).Wait();
                    }
                }
            }
        }

        static void UnhandledExceptionTrapper(object sender, UnhandledExceptionEventArgs e) {
            Console.WriteLine(e.ExceptionObject.ToString());
            Environment.Exit(1);
        }
    }
}

