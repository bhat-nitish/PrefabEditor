namespace Editor
{
    public class PrefabConfigEditorState
    {
        public bool HasConfiguratonError { get; set; }

        public bool IsInvalidConfiguration { get; set; }

        public bool IsValidConfiguration { get; set; }

        public string CurrentConfigurationFileName { get; set; }

        public string ConfigSearchString { get; set; }

        public string CurrentConfigurationForPreview { get; set; }

        public string CurrentConfigurationForPreviewText { get; set; }

        public bool HasValidConfigurationSelected => !string.IsNullOrWhiteSpace(CurrentConfigurationForPreviewText);

        public void ClearConfigErrorsAndWarnings()
        {
            IsInvalidConfiguration = false;
            HasConfiguratonError = false;
            CurrentConfigurationFileName = string.Empty;
        }

        public void ReSetCurrentConfigurationForPreview()
        {
            CurrentConfigurationForPreview = string.Empty;
            CurrentConfigurationForPreviewText = string.Empty;
        }
    }
}