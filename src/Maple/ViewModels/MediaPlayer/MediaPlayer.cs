using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Input;
using FluentValidation;
using Maple.Core;
using Maple.Interfaces;
using Maple.Localization.Properties;

namespace Maple
{
    [DebuggerDisplay("{Name}, {Sequence}")]
    public class MediaPlayer : ValidableBaseDataViewModel<MediaPlayer, Data.MediaPlayer>, IDisposable, IChangeState, ISequence
    {
        private List<SubscriptionToken> _messageTokens;
        protected readonly ILocalizationService _manager;

        public bool IsPlaying { get { return Player.IsPlaying; } }
        public bool Disposed { get; private set; }

        public bool IsNew => Model.IsNew;
        public bool IsDeleted => Model.IsDeleted;

        private IMediaPlayer _player;
        public IMediaPlayer Player
        {
            get { return _player; }
            private set { SetValue(ref _player, value); }
        }

        public ICommand PlayCommand { get; private set; }
        public ICommand PauseCommand { get; private set; }
        public ICommand NextCommand { get; private set; }
        public ICommand PreviousCommand { get; private set; }
        public ICommand StopCommand { get; private set; }
        public ICommand LoadFromFileCommand { get; private set; }
        public ICommand LoadFromFolderCommand { get; private set; }
        public ICommand LoadFromUrlCommand { get; private set; }
        public ICommand RemoveRangeCommand { get; protected set; }
        public ICommand RemoveCommand { get; protected set; }
        public ICommand ClearCommand { get; protected set; }

        private AudioDevices _audioDevices;
        public AudioDevices AudioDevices
        {
            get { return _audioDevices; }
            private set { SetValue(ref _audioDevices, value); }
        }

        private Playlist _playlist;
        public Playlist Playlist
        {
            get { return _playlist; }
            set { SetValue(ref _playlist, value, OnChanging: OnPlaylistChanging, OnChanged: OnPlaylistChanged); }
        }

        private int _sequence;
        public int Sequence
        {
            get { return _sequence; }
            set { SetValue(ref _sequence, value, OnChanged: () => Model.Sequence = value); }
        }

        private string _name;
        public string Name
        {
            get { return _name; }
            set { SetValue(ref _name, value, OnChanged: () => Model.Name = value); }
        }

        private bool _isPrimary;
        public bool IsPrimary
        {
            get { return _isPrimary; }
            protected set { SetValue(ref _isPrimary, value, OnChanged: () => Model.IsPrimary = value); }
        }

        private string _createdBy;
        public string CreatedBy
        {
            get { return _createdBy; }
            set { SetValue(ref _createdBy, value, OnChanged: () => Model.CreatedBy = value); }
        }

        private string _updatedBy;
        public string UpdatedBy
        {
            get { return _updatedBy; }
            set { SetValue(ref _updatedBy, value, OnChanged: () => Model.UpdatedBy = value); }
        }

        private DateTime _updatedOn;
        public DateTime UpdatedOn
        {
            get { return _updatedOn; }
            set { SetValue(ref _updatedOn, value, OnChanged: () => Model.UpdatedOn = value); }
        }

        private DateTime _createdOn;
        public DateTime CreatedOn
        {
            get { return _updatedOn; }
            set { SetValue(ref _updatedOn, value, OnChanged: () => Model.CreatedOn = value); }
        }

        public MediaPlayer(ViewModelServiceContainer container, IMediaPlayer player, IValidator<MediaPlayer> validator, AudioDevices devices, Playlist playlist, Data.MediaPlayer model)
            : base(model, validator, container.Messenger)
        {
            _manager = container.LocalizationService;
            Player = player ?? throw new ArgumentNullException(nameof(player), $"{nameof(player)} {Resources.IsRequired}");

            _name = model.Name;
            _audioDevices = devices;
            _sequence = model.Sequence;

            _createdBy = model.CreatedBy;
            _createdOn = model.CreatedOn;
            _updatedBy = model.UpdatedBy;
            _updatedOn = model.UpdatedOn;

            if (AudioDevices.Items.Count > 0)
                Player.AudioDevice = AudioDevices.Items.FirstOrDefault(p => p.Name == Model.DeviceName) ?? AudioDevices.Items[0];

            InitializeSubscriptions();
            InitiliazeCommands();

            Validate();
        }

        private void InitializeSubscriptions()
        {
            _messageTokens = new List<SubscriptionToken>
            {
                Messenger.Subscribe<PlayingMediaItemMessage>(Player_PlayingMediaItem, IsSenderEqualsPlayer),
                Messenger.Subscribe<CompletedMediaItemMessage>(MediaPlayer_CompletedMediaItem, IsSenderEqualsPlayer),
                Messenger.Subscribe<ViewModelSelectionChangingMessage<AudioDevice>>(Player_AudioDeviceChanging, IsSenderEqualsPlayer),
                Messenger.Subscribe<ViewModelSelectionChangingMessage<AudioDevice>>(Player_AudioDeviceChanged, IsSenderEqualsPlayer),
            };
        }

        private void InitiliazeCommands()
        {
            PlayCommand = new RelayCommand<MediaItem>(Player.Play, CanPlay);
            PreviousCommand = new RelayCommand(Previous, () => Playlist?.CanPrevious() == true && CanPrevious());
            NextCommand = new RelayCommand(Next, () => Playlist?.CanNext() == true && CanNext());
            PauseCommand = new RelayCommand(Pause, () => CanPause());
            StopCommand = new RelayCommand(Stop, () => CanStop());
            RemoveCommand = new RelayCommand<MediaItem>(Remove, CanRemove);
            ClearCommand = new RelayCommand(Clear, CanClear);

            UpdatePlaylistCommands();
        }

        private bool IsSenderEqualsPlayer(object sender)
        {
            return ReferenceEquals(sender, Player);
        }

        private void Player_AudioDeviceChanging(ViewModelSelectionChangingMessage<AudioDevice> e)
        {
            // TODO
        }

        private void Player_AudioDeviceChanged(ViewModelSelectionChangingMessage<AudioDevice> e)
        {
            if (!string.IsNullOrEmpty(e?.Content?.Name))
                Model.DeviceName = e.Content.Name;
        }

        private void OnPlaylistChanging()
        {
            Stop();
        }

        private void OnPlaylistChanged()
        {
            Model.Playlist.Id = Playlist.Id;
            // TODO: maybe add optional endless playback

            UpdatePlaylistCommands();
        }

        private void UpdatePlaylistCommands()
        {
            if (Playlist != null)
            {
                LoadFromFileCommand = Playlist.LoadFromFileCommand;
                LoadFromFolderCommand = Playlist.LoadFromFolderCommand;
                LoadFromUrlCommand = Playlist.LoadFromUrlCommand;
            }
        }

        private void Player_PlayingMediaItem(PlayingMediaItemMessage e)
        {
            // TODO: sync state to other viewmodels
        }

        private void MediaPlayer_CompletedMediaItem(CompletedMediaItemMessage e)
        {
            Next();
        }

        /// <summary>
        /// Clears this instance.
        /// </summary>
        public void Clear()
        {
            using (BusyStack.GetToken())
                Playlist.Clear();
        }

        /// <summary>
        /// Determines whether this instance can clear.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can clear; otherwise, <c>false</c>.
        /// </returns>
        public bool CanClear()
        {
            return !IsBusy && Playlist.Count > 0;
        }

        /// <summary>
        /// Adds the range.
        /// </summary>
        /// <param name="mediaItems">The media items.</param>
        public void AddRange(IEnumerable<MediaItem> mediaItems)
        {
            using (BusyStack.GetToken())
            {
                foreach (var item in mediaItems)
                    Playlist.Add(item);
            }
        }

        /// <summary>
        /// Adds the specified media item.
        /// </summary>
        /// <param name="mediaItem">The media item.</param>
        public void Add(MediaItem mediaItem)
        {
            using (BusyStack.GetToken())
            {
                if (Playlist.Items.Any())
                {
                    var maxIndex = Playlist.Items.Max(p => p.Sequence) + 1;
                    if (maxIndex < 0)
                        maxIndex = 0;

                    mediaItem.Sequence = maxIndex;
                }
                else
                    mediaItem.Sequence = 0;

                Playlist.Items.Add(mediaItem);
            }
        }

        /// <summary>
        /// Removes the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        public void Remove(MediaItem item)
        {
            using (BusyStack.GetToken())
                Playlist.Remove(item);
        }

        private bool CanRemove(MediaItem item)
        {
            using (BusyStack.GetToken())
                return Playlist.CanRemove(item);
        }

        /// <summary>
        /// Pauses this instance.
        /// </summary>
        public void Pause()
        {
            using (BusyStack.GetToken())
                Player.Pause();
        }

        /// <summary>
        /// Stops this instance.
        /// </summary>
        public void Stop()
        {
            using (BusyStack.GetToken())
                Player.Stop();
        }

        /// <summary>
        /// Previouses this instance.
        /// </summary>
        public void Previous()
        {
            using (BusyStack.GetToken())
            {
                var item = Playlist.Previous();
                Player.Play(item);
            }
        }

        /// <summary>
        /// Nexts this instance.
        /// </summary>
        public void Next()
        {
            using (BusyStack.GetToken())
            {
                var item = Playlist.Next();
                Player.Play(item);
            }
        }

        /// <summary>
        /// Determines whether this instance can next.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can next; otherwise, <c>false</c>.
        /// </returns>
        public bool CanNext()
        {
            var item = Playlist.Next();
            return CanPlay(item);
        }

        /// <summary>
        /// Determines whether this instance can previous.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can previous; otherwise, <c>false</c>.
        /// </returns>
        public bool CanPrevious()
        {
            var item = Playlist.Previous();
            return CanPlay(item);
        }

        /// <summary>
        /// Determines whether this instance can pause.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can pause; otherwise, <c>false</c>.
        /// </returns>
        public bool CanPause()
        {
            return Player.CanPause();
        }

        /// <summary>
        /// Determines whether this instance can stop.
        /// </summary>
        /// <returns>
        ///   <c>true</c> if this instance can stop; otherwise, <c>false</c>.
        /// </returns>
        public bool CanStop()
        {
            return Player.CanStop();
        }

        /// <summary>
        /// Determines whether this instance can play the specified item.
        /// </summary>
        /// <param name="item">The item.</param>
        /// <returns>
        ///   <c>true</c> if this instance can play the specified item; otherwise, <c>false</c>.
        /// </returns>
        private bool CanPlay(MediaItem item)
        {
            return Player.CanPlay(item);
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (Disposed)
                return;

            if (IsPlaying)
                Stop();

            if (disposing)
            {
                foreach (var token in _messageTokens)
                    Messenger.Unsubscribe<IMapleMessage>(token);

                if (Player != null)
                {
                    Player?.Dispose();
                    Player = null;
                }
                // Free any other managed objects here.
            }

            // Free any unmanaged objects here.
            Disposed = true;
        }
    }
}
