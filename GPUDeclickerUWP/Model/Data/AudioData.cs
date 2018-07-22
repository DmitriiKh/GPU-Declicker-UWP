using GPUDeclickerUWP.Model.Processing;

namespace GPUDeclickerUWP.Model.Data
{
    public enum ChannelType
    {
        Left,
        Right
    }

    public abstract class AudioData
    {
        internal AudioChannel CurrentAudioChannel;
        internal bool IsStereo;

        public AudioProcessingSettings AudioProcessingSettings { get; protected set; }

        public abstract ChannelType GetCurrentChannelType();
        public abstract void SetCurrentChannelType(ChannelType channelType);
        public abstract void ClearAllClicks();
        public abstract void SortClicks();

        public int LengthSamples()
        {
            return CurrentAudioChannel.LengthSamples();
        }


        public void CurrentChannelRestoreInitState(int position, int lenght)
        {
            CurrentAudioChannel.RestoreInitState(position, lenght);
        }

        public void SetCurrentChannelIsPreprocessed()
        {
            CurrentAudioChannel.ChannelIsPreprocessed = true;
        }

        public bool CurrentChannelIsPreprocessed()
        {
            return CurrentAudioChannel.ChannelIsPreprocessed;
        }

        public void BackupCurrentChannelPredErrors()
        {
            for (var index = 0;
                index < CurrentAudioChannel.LengthSamples();
                index++)
                CurrentAudioChannel.SetPredictionErrBackup(
                    index,
                    CurrentAudioChannel.GetPredictionErr(index));
        }

        public void RestoreCurrentChannelPredErrors()
        {
            for (var index = 0;
                index < CurrentAudioChannel.LengthSamples();
                index++)
                CurrentAudioChannel.SetPredictionErr(
                    index,
                    CurrentAudioChannel.GetPredictionErrBackup(index));
        }

        public void AddClickToList(AudioClick audioClick)
        {
            CurrentAudioChannel.AddClickToList(audioClick);
        }

        public int CurrentChannelGetNumberOfClicks()
        {
            return CurrentAudioChannel.GetNumberOfClicks();
        }

        public AudioClick GetClick(int index)
        {
            return CurrentAudioChannel.GetClick(index);
        }

        public float GetInputSample(int position)
        {
            return CurrentAudioChannel.GetInputSample(position);
        }

        public void SetInputSample(int position, float sample)
        {
            CurrentAudioChannel.SetInputSample(position, sample);
        }

        public float GetOutputSample(int position)
        {
            return CurrentAudioChannel.GetOutputSample(position);
        }

        public void SetOutputSample(int position, float sample)
        {
            CurrentAudioChannel.SetOutputSample(position, sample);
        }

        public float GetPredictionErr(int position)
        {
            return CurrentAudioChannel.GetPredictionErr(position);
        }

        public void SetPredictionErr(int position, float prediction)
        {
            CurrentAudioChannel.SetPredictionErr(position, prediction);
        }

        public float GetErrorAverage(int position)
        {
            return CurrentAudioChannel.GetPredictionErrAverage(position);
        }

        public void SetErrorAverage(int position, float aAverage)
        {
            CurrentAudioChannel.SetPredictionErrAverage(position, aAverage);
        }

        public void OnClickChanged(object source, ClickEventArgs e)
        {
            var click = source as AudioClick;
            if (click is null)
                return;

            if (e.Shrinked)
            {
                SetOutputSample(
                    click.Position - 1,
                    GetInputSample(click.Position - 1));
                SetOutputSample(
                    click.Position + click.Length,
                    GetInputSample(click.Position + click.Length));
            }

            e.ErrorLevelDetected = ClickRepairer.Repair(
                this,
                click.Position,
                click.Length);
        }
    }
}