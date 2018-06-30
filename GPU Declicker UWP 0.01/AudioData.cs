namespace GPU_Declicker_UWP_0._01
{
    public enum ChannelType
    {
        Left,
        Right
    }

    public abstract class AudioData
    {
        internal AudioChannel currentAudioChannel;
        internal bool IsStereo;

        public AudioProcessingSettings AudioProcessingSettings { get; set; }

        public abstract ChannelType GetCurrentChannelType();
        public abstract void SetCurrentChannelType(ChannelType channelType);
        public abstract void ClearAllClicks();
        public abstract void SortClicks();

        public int LengthSamples()
        {
            return currentAudioChannel.LengthSamples();
        }


        public void CurrentChannelRestoreInitState(int position, int lenght)
        {
            currentAudioChannel.RestoreInitState(position, lenght);
        }

        public float GetPredictionErrBackup(int position)
        {
            return currentAudioChannel.GetPredictionErrBackup(position);
        }

        public void SetCurrentChannelIsPreprocessed()
        {
            currentAudioChannel.ChannelIsPreprocessed = true;
        }

        public bool CurrentChannelIsPreprocessed()
        {
            return currentAudioChannel.ChannelIsPreprocessed;
        }

        public void BackupCurrentChannelPredErrors()
        {
            for (var index = 0;
                index < currentAudioChannel.LengthSamples();
                index++)
                currentAudioChannel.SetPredictionErrBackup(
                    index,
                    currentAudioChannel.GetPredictionErr(index));
        }

        public void RestoreCurrentChannelPredErrors()
        {
            for (var index = 0;
                index < currentAudioChannel.LengthSamples();
                index++)
                currentAudioChannel.SetPredictionErr(
                    index,
                    currentAudioChannel.GetPredictionErrBackup(index));
        }

        public void AddClickToList(
            int position,
            int lenght,
            float threshold_level_detected)
        {
            currentAudioChannel.AddClickToList(
                position, lenght,
                threshold_level_detected,
                this,
                GetCurrentChannelType());
        }

        public int CurrentChannelGetNumberOfClicks()
        {
            return currentAudioChannel.GetNumberOfClicks();
        }

        public void ChangeClickAproved(int index)
        {
            currentAudioChannel.ChangeClickAproved(index);
        }

        public AudioClick GetClick(int index)
        {
            return currentAudioChannel.GetClick(index);
        }

        public AudioClick GetLastClick()
        {
            return currentAudioChannel.GetLastClick();
        }

        public float GetInputSample(int position)
        {
            return currentAudioChannel.GetInputSample(position);
        }

        public void SetInputSample(int position, float sample)
        {
            currentAudioChannel.SetInputSample(position, sample);
        }

        public float GetOutputSample(int position)
        {
            return currentAudioChannel.GetOutputSample(position);
        }

        public void SetOutputSample(int position, float sample)
        {
            currentAudioChannel.SetOutputSample(position, sample);
        }

        public float GetPredictionErr(int position)
        {
            return currentAudioChannel.GetPredictionErr(position);
        }

        public void SetPredictionErr(int position, float prediction)
        {
            currentAudioChannel.SetPredictionErr(position, prediction);
        }

        public float GetErrorAverage(int position)
        {
            return currentAudioChannel.GetPredictionErrAverage(position);
        }

        public void SetErrorAverage(int position, float a_average)
        {
            currentAudioChannel.SetPredictionErrAverage(position, a_average);
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

            e.ThresholdLevelDetected = ClickRepairer.Repair(
                this,
                click.Position,
                click.Length);
        }
    }
}