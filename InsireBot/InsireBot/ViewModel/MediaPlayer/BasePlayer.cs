﻿using InsireBot.Core;

namespace InsireBot
{
    public abstract class BasePlayer : ObservableObject, IMediaPlayer
    {
        public event CompletedMediaItemEventHandler CompletedMediaItem;
        public event PlayingMediaItemEventHandler PlayingMediaItem;

        event CompletedMediaItemEventHandler IMediaPlayer.CompletedMediaItem
        {
            add { CompletedMediaItem += value; }
            remove { CompletedMediaItem -= value; }
        }

        event PlayingMediaItemEventHandler IMediaPlayer.PlayingMediaItem
        {
            add { PlayingMediaItem += value; }
            remove { PlayingMediaItem -= value; }
        }

        private AudioDevice _audioDevice;
        public AudioDevice AudioDevice
        {
            get { return _audioDevice; }
            set { SetValue(ref _audioDevice, value); }
        }

        private IMediaItem _current;
        public IMediaItem Current
        {
            get { return _current; }
            set { SetValue(ref _current, value); }
        }

        private bool _disposed;
        public bool Disposed
        {
            get { return _disposed; }
            protected set { SetValue(ref _disposed, value); }
        }

        public abstract bool IsPlaying { get; }

        public abstract int Volume { get; set; }

        public abstract int VolumeMax { get; }

        public abstract int VolumeMin { get; }

        public abstract void Play(IMediaItem mediaItem);

        public abstract void Pause();

        public abstract void Stop();

        public abstract void Dispose();

        public abstract bool CanPlay(IMediaItem item);

        public virtual bool CanStop()
        {
            return false;
        }

        public virtual bool CanPause()
        {
            return false;
        }
    }
}
