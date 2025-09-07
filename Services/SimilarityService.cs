using Microsoft.ML;
using System;
using System.Linq;

namespace ThesisNest.Services
{
    public class SimilarityService
    {
        private readonly MLContext _ml = new MLContext(seed: 0);

        public float ComputeCosineSimilarity(string textA, string textB)
        {
            textA ??= "";
            textB ??= "";

            var data = new[]
            {
                new TextData{ Text = textA },
                new TextData{ Text = textB }
            };

            var dv = _ml.Data.LoadFromEnumerable(data);
            var pipeline = _ml.Transforms.Text.FeaturizeText(outputColumnName: "Features", inputColumnName: nameof(TextData.Text));
            var transformer = pipeline.Fit(dv);
            var transformed = transformer.Transform(dv);

            var preds = _ml.Data.CreateEnumerable<TransformedData>(transformed, reuseRowObject: false).ToArray();

            var vA = preds[0].Features ?? Array.Empty<float>();
            var vB = preds[1].Features ?? Array.Empty<float>();

            return Cosine(vA, vB);
        }

        private float Cosine(float[] a, float[] b)
        {
            if (a.Length != b.Length || a.Length == 0) return 0f;
            double dot = 0, na = 0, nb = 0;
            for (int i = 0; i < a.Length; i++)
            {
                dot += a[i] * b[i];
                na += a[i] * a[i];
                nb += b[i] * b[i];
            }
            var denom = Math.Sqrt(na) * Math.Sqrt(nb) + 1e-10;
            return (float)(dot / denom);
        }

        private class TextData { public string Text { get; set; } = ""; }
        private class TransformedData { public float[] Features { get; set; } }
    }
}
