using System.Collections.Generic;

namespace ThesisNest.ViewModels
{
    public class PlagiarismResultViewModel
    {
        public int DocumentId { get; set; }
        public string FileName { get; set; } = "";
        public float LocalMaxSimilarity { get; set; } // %
        public int LocalBestMatchDocumentId { get; set; }
        public float CombinedScore { get; set; } // %
        public List<WebMatch> WebMatches { get; set; } = new();
    }

    public class WebMatch
    {
        public string Sentence { get; set; } = "";
        public string Snippet { get; set; } = "";
        public string Url { get; set; } = "";
        public float Score { get; set; } // %
    }
}
