using Microsoft.ML.OnnxRuntime;
using Microsoft.ML.OnnxRuntime.Tensors;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Runtime.CompilerServices;
using System.Text;
using System.Linq;
using ContentSafetyGuard.Services;


namespace ContentSafetyGuard.AI
{
    internal class NsfwDetector
    {

        // Klassen
        ImagePreprocessor imagepreprocessor = new ImagePreprocessor();
        private readonly Lazy<InferenceSession> modelSession; // wird erst beim ersten Scan geladen
        private ImageTiler imageTiler = new ImageTiler(); 

        // Instanzfelder

        // Konstanten
        private const string ModelInputName = "pixel_values"; // Name des Modell-Inputs 
        private const string ModelOutputName = "logits"; // Name des Outputs
        private const int ModelWidth = 224; // Modellgröße breite
        private const int ModelHeight = 224; // und höhe
        private float NsfwThreshold;

        public void SetNsfwThreshold(float threshold)
        {
            if (threshold < 0f)
            {
                threshold = 0f;
            }

            if (threshold > 1f)
            {
                threshold = 1f;
            }

            NsfwThreshold = threshold;
        }

        public NsfwDetector(float nsfwthreshold)
        {
            string modelPath = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/Models", "nsfw-model.onnx");
            modelSession = new Lazy<InferenceSession>(() => new InferenceSession(modelPath));

            SetNsfwThreshold(nsfwthreshold);
        }  // Konstruktor 

        private float CalculateNsfwScore(float normalLogit, float nsfwLogit)
        {
            float normalExp = MathF.Exp(normalLogit);
            float nsfwExp = MathF.Exp(nsfwLogit);

            return nsfwExp / (normalExp + nsfwExp);
        }

        // Ein Frame prüfen
        public bool DetectSingleFrame(Bitmap? frame) // Hilfsmethode prüft auf jedes einzelne Bild nach NSFW
        {

            if (frame == null)
            {
                return false;
            }

            using Bitmap resizeframe = imagepreprocessor.ResizeCapturedFrame(frame, 224, 224);
            DenseTensor<float> inputTensor = imagepreprocessor.BitmapToTensor(resizeframe);

            List<NamedOnnxValue> inputs = new List<NamedOnnxValue>();

            inputs.Add(NamedOnnxValue.CreateFromTensor(ModelInputName, inputTensor));

            using var outputs = modelSession.Value.Run(inputs);

            var primaryOutput = outputs.First(output => output.Name == ModelOutputName);

            var logitTensors = primaryOutput.AsTensor<float>();

            float normalLogit = logitTensors[0, 0]; 
            float nsfwLogit = logitTensors[0, 1];   

            var Score = CalculateNsfwScore(normalLogit, nsfwLogit);
            //System.Diagnostics.Debug.WriteLine($"NSFW Score: {Score}");

            return Score >= NsfwThreshold; // Threshold hoch schwerer zu blockieren 
        }

        public bool DetectExplicitContentFromFrameviaGrid(Bitmap? frame)
        {
            if (frame == null)
            {
                return false;
            }

            List<Bitmap> tiles = imageTiler.CreateGridTilesWithCenter(frame);
            //tiles.Add(frame); // Für große bilder


            int detected = 0;

            // Mehrere Kerne nutzen für Grid
            try
            {
                ParallelOptions options = new ParallelOptions
                {
                    MaxDegreeOfParallelism = 2
                };

                Parallel.ForEach(tiles, options, (tile, state) =>
                {
                    if (Volatile.Read(ref detected) == 1)
                    {
                        state.Stop();
                        return;
                    }

                    if (DetectSingleFrame(tile))
                    {
                        Interlocked.Exchange(ref detected, 1);
                        state.Stop();
                    }
                });

                return Volatile.Read(ref detected) == 1;
            }
            finally
            {
                foreach (Bitmap tile in tiles)
                {
                    tile.Dispose();
                }
            }

        }



    }

}
