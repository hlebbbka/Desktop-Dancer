using System.IO;
using System.Windows;
using System.Windows.Controls;
namespace MusicDancer;
public partial class MainWindow
{
    private string language="ru",audioStatus="Подключение...";
    private bool localizationReady,applyingLanguage;
    private string T(string text)=>UiText.Get(text,language);
    private void InitializeLocalization(){ApplyLanguage("ru");localizationReady=true;}
    private void Language_SelectionChanged(object sender,SelectionChangedEventArgs e)
    {
        if(!localizationReady||applyingLanguage)return;
        ApplyLanguage(LanguageChoice.SelectedValue as string);
        if(!profileTrail)SaveSettings();
    }
    private void ApplyLanguage(string? value)
    {
        language=UiText.Normalize(value);applyingLanguage=true;
        try{
            foreach(var entry in UiText.Entries)Resources["Ui."+entry.Key]=language=="en"?entry.English:entry.Russian;
            LanguageChoice.SelectedValue=language;
            StatusText.Text=LocalizedAudioStatus();
            ToggleButton.Content=T(paused?"▶  Продолжить":"⏸  Пауза");
            PetButton.Content=T(pet==null?"✦  Вывести питомца":"✕  Убрать питомца");
            CharacterName.Text=CharacterDisplayName(imagePath);
            PartnerLabel.Text=partnerPath==null?T("Выберите партнёра"):CharacterDisplayName(partnerPath);
            foreach(var panel in new[]{CharacterGallery,PartnerGallery})foreach(var child in panel.Children)
                if(child is Button button&&button.Tag is string name)button.ToolTip=T(name);
            // These properties are assigned by live data, so their resource
            // expressions may have been replaced since the last language change.
            BpmText.Text=TranslateCurrentLabel(BpmText.Text);
            LevelText.Text=TranslateCurrentLabel(LevelText.Text);
            SpotifyTrack.Text=TranslateCurrentLabel(SpotifyTrack.Text);
            LyricsSourceStatus.Text=T(LyricsLayer.Visibility==Visibility.Visible&&LyricsCurrent.Text.Length>0?"Текст синхронизирован":"Текст не синхронизирован");
            pet?.SetLanguage(language);
        }finally{applyingLanguage=false;}
    }
    private string TranslateCurrentLabel(string text)
    {
        foreach(var entry in UiText.Entries)if(text==entry.Russian||text==entry.English)return T(entry.Russian);
        return text;
    }
    private string CharacterDisplayName(string? path)=>T(path==null?"Кот в деловом костюме":Path.GetFileNameWithoutExtension(path));
    private string LocalizedAudioStatus()
    {
        const string prefix="Звук недоступен: ";
        return audioStatus.StartsWith(prefix,StringComparison.Ordinal)?T(prefix)+T(audioStatus[prefix.Length..]):T(audioStatus);
    }
}
