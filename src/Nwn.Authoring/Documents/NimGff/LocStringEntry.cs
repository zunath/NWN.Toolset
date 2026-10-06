using System.Globalization;
using System.Text;
using NwnResRef = Nwn.Formats.Resources.ResourceReferenceRules;
using Nwn.Authoring.Editing;

namespace Nwn.Authoring.Documents.NimGff
{
    /// <summary>
    /// One localized-string entry of a cexolocstring value: a language key (e.g. "0") and the
    /// raw string token holding its text.
    /// </summary>
    public sealed class LocStringEntry
    {
        public string LanguageKey { get; }
        public byte[] RawText { get; internal set; }
        public bool PreferUtf8Text { get; internal set; }

        public LocStringEntry(string languageKey, byte[] rawText, bool preferUtf8Text = false)
        {
            LanguageKey = languageKey;
            RawText = rawText;
            PreferUtf8Text = preferUtf8Text;
        }

        public string GetText()
        {
            return JsonStringCodec.Decode(RawText);
        }

        /// <summary>Updates localized text while preserving unchanged tokens and the encoding of imported non-ASCII text.</summary>
        public void SetText(string text)
        {
            EditScope.EnsureMutationAllowed(this);
            var oldRawText = RawText;
            RawText = JsonStringCodec.EncodeReplacement(text, oldRawText, PreferUtf8Text);
            if (ReferenceEquals(oldRawText, RawText))
                return;
            EditScope.Capture(this, new LocStringEntryTextEdit(this, oldRawText, RawText));
        }
    }

}
