using System.Collections.Generic;

namespace Editor
{
    public class PrefabConfigEditorState
    {
        public bool HasConfiguratonError { get; set; }

        public bool IsInvalidConfiguration { get; set; }

        public bool IsValidConfiguration { get; set; }

        public string CurrentConfigurationFileName { get; set; }

        public string ConfigSearchString { get; set; }

        public string PrefabSearchString { get; set; }

        public string CurrentConfigurationForPreview { get; set; }

        public string CurrentConfigurationForPreviewText { get; set; }

        public bool HasValidConfigurationSelected => !string.IsNullOrWhiteSpace(CurrentConfigurationForPreviewText);

        public bool HasPrefabSearchText => !string.IsNullOrWhiteSpace(PrefabSearchString);

        public bool ShouldDisplayPrefabResults =>
            HasPrefabSearchText || ShowAllPrefabsInDirectory || ShowAllPrefabsInProject;

        public bool ShowAllPrefabsInProject { get; set; }

        public bool ShowAllPrefabsInDirectory { get; set; }

        public List<string> SelectedPrefabGuids { get; set; }

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

        public PrefabConfigEditorState()
        {
            SelectedPrefabGuids = new List<string>();
        }

        public void TogglePrefabSelection(bool isSelected, string guid)
        {
            if (string.IsNullOrWhiteSpace(guid))
            {
                return;
            }

            if (isSelected)
            {
                if (SelectedPrefabGuids.Contains(guid))
                {
                    return;
                }

                SelectedPrefabGuids.Add(guid);
            }
            else
            {
                SelectedPrefabGuids.RemoveAll(id => id == guid);
            }
        }
    }
}