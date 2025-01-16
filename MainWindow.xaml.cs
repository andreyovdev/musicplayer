using Microsoft.Win32;

using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

using NAudio.Wave;
using MaterialDesignThemes.Wpf;
using Path = System.IO.Path;

namespace Music_App
{
	public partial class MainWindow : Window
	{
		private static List<string> songList = new List<string>();
		private int currentSongIndex = 0;
		private string currentSongPath = string.Empty;
		private string currentImagePath = string.Empty;
		private DispatcherTimer timer;
		private bool isMusicLoaded = false;
		private bool isMediaPlaying = false;
		private bool isDraggingSlider = false;
		private bool isPlaylistUpdated = false;
		private PackIcon packIcon;
		private ImageSource defaultImageSource;

		public MainWindow()
		{
			InitializeComponent();

			currentImagePath = $"{Directory.GetParent(Directory.GetCurrentDirectory()).Parent.Parent.ToString()}\\Images\\MusicIcon.png";
			Musicimg.Source = new BitmapImage(new Uri(currentImagePath));
			defaultImageSource = Musicimg.Source;

			timer = new DispatcherTimer();
			timer.Interval = TimeSpan.FromMilliseconds(500);
			timer.Tick += (s, e) => UpdateSliderPosition();
			timer.Tick += (s, e) => UpdateTimingPosition();
			timer.Tick += (s, e) => UpdateSongEnded();
			timer.Tick += (s, e) => UpdateVolumeSliderPosition();
			timer.Start();
		}

		//Button events
		private void Card_MouseDown(object sender, MouseButtonEventArgs e)
		{
			DragMove();
		}

		private void btnClose_Click(object sender, RoutedEventArgs e)
		{
			Application.Current.Shutdown();
		}

		private void btnFile_Click(object sender, RoutedEventArgs e)
		{
			OpenFileDialog openFileDialog = new OpenFileDialog
			{
				Filter = "MP3 Files|*.mp3",
				Title = "Select Music Files",
				Multiselect = true
			};

			//Debug
			if (openFileDialog.ShowDialog() == true)
			{
				Console.WriteLine("File dialog opened.");
			}

			foreach (var filePath in openFileDialog.FileNames)
			{
				songList.Add(filePath);
			}

			if (openFileDialog.FileNames.Length == 0)
				return;

			currentSongPath = songList[0];

			LoadSong();

			foreach (var song in songList)
			{
				playlistListBox.Items.Add(System.IO.Path.GetFileNameWithoutExtension(song));
			}

			ViewSongInfo();

			playlistListBox.SelectedIndex = currentSongIndex;

			StackPanel stackPanel = (StackPanel)btnPlay.Content;
			packIcon = (PackIcon)stackPanel.Children[0];

			if (!isPlaylistUpdated)
			{
				isPlaylistUpdated = true;
			}
		}

		private void btnPlay_Click(object sender, RoutedEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			PlayPause();
		}

		private void btnPNext_Click(object sender, RoutedEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			NavigateToSong("next");
		}

		private void btnPPrevious_Click(object sender, RoutedEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			NavigateToSong("previous");

		}

		private void btnPForward_Click(object sender, RoutedEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			TimeSpan newPosition = mediaPlayer.Position + TimeSpan.FromSeconds(5);

			if (newPosition < GetMp3Duration(currentSongPath))
			{
				mediaPlayer.Position = newPosition;
				lblCurrenttime.Text = GetFormatedTime(mediaPlayer.Position);
				TimerSlider.Value = newPosition.TotalSeconds;
			}
			else
			{
				mediaPlayer.Position = GetMp3Duration(currentSongPath);
			}

		}

		private void btnPRewind_Click(object sender, RoutedEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			TimeSpan newPosition = mediaPlayer.Position - TimeSpan.FromSeconds(5);


			if (newPosition >= TimeSpan.Zero)
			{
				mediaPlayer.Position = newPosition;
				lblCurrenttime.Text = GetFormatedTime(mediaPlayer.Position);
				TimerSlider.Value = newPosition.TotalSeconds;
			}
			else
			{
				mediaPlayer.Position = TimeSpan.Zero;
			}
		}

		//Time slider
		private void TimerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
		{
			if (!isMusicLoaded)
				return;

			if (isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
			{
				mediaPlayer.Position = TimeSpan.FromSeconds(TimerSlider.Value);
			}
		}

		private void TimerSlider_PreviewMouseDown(object sender, MouseButtonEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			mediaPlayer.Pause();

			isDraggingSlider = true;
		}

		private void TimerSlider_PreviewMouseUp(object sender, MouseButtonEventArgs e)
		{
			if (!isMusicLoaded)
				return;

			isDraggingSlider = false;

			TimeSpan curTime = TimeSpan.FromSeconds(TimerSlider.Value);
			mediaPlayer.Position = curTime;
			lblCurrenttime.Text = GetFormatedTime(curTime);

			if (packIcon.Kind == PackIconKind.Pause)
				mediaPlayer.Play();
		}

		private void UpdateSliderPosition()
		{
			if (!isMusicLoaded)
				return;

			if (!isDraggingSlider && mediaPlayer.NaturalDuration.HasTimeSpan)
			{
				TimerSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
				TimerSlider.Value = mediaPlayer.Position.TotalSeconds;
			}
		}

		//Volume slider
		private void UpdateVolumeSliderPosition()
		{
			mediaPlayer.Volume = VolumeSlider.Value;
		}

		//Song selection
		private void PlaylistListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
		{
			if (playlistListBox.SelectedItem == null)
			{
				playlistListBox.SelectedItem = playlistListBox.Items[0];
			}

			string? path = songList.Find(s => s.Contains(playlistListBox.SelectedItem.ToString()!));
			if (path == null)
				return;

			currentSongIndex = songList.IndexOf(path);
			currentSongPath = path;

			LoadSong();
			ViewSongInfo();
		}

		//Services
		private void Timer_Tick(object sender, EventArgs e)
		{

		}

		private void UpdateSongEnded()
		{
			if (isMediaPlaying && mediaPlayer.Position.TotalSeconds >= GetMp3Duration(currentSongPath).TotalSeconds - 1)
			{
				NavigateToSong("next");
			}
		}

		private void UpdateTimingPosition()
		{
			lblCurrenttime.Text = GetFormatedTime(mediaPlayer.Position);
		}

		static TimeSpan GetMp3Duration(string filePath)
		{
			try
			{
				using (var reader = new Mp3FileReader(filePath))
				{
					return reader.TotalTime;
				}
			}
			catch (Exception ex)
			{
				return TimeSpan.Zero;
			}
		}

		private string GetFormatedTime(TimeSpan ts)
		{
			string formattedTime;

			if (ts.Hours > 0)
			{
				formattedTime = ts.ToString(@"h\:mm\:ss");
			}
			else
			{
				formattedTime = ts.ToString(@"m\:ss");
			}

			return formattedTime;
		}

		private string GetSongArtist()
		{
			string path = System.IO.Path.GetFileNameWithoutExtension(currentSongPath);
			if (path.Contains(" - "))
			{
				return path.Split(" - ")[0];
			}
			return "Unknown";
		}

		private string GetSongName()
		{
			string path = System.IO.Path.GetFileNameWithoutExtension(currentSongPath);
			if (path.Contains(" - "))
			{
				return path.Split(" - ")[1];
			}
			return path;
		}

		private void ViewSongInfo()
		{
			var duration = GetMp3Duration(currentSongPath);

			//Song image
			Musicimg.Source = GetAlbumCoverImage() != null ? GetAlbumCoverImage() : defaultImageSource;
			//Song artist
			lblSongartist.Text = GetSongArtist();
			//Song name
			lblSongname.Text = GetSongName();
			//Song length time
			lblMusiclength.Text = GetFormatedTime(duration);
			//Slider length time
			TimerSlider.Maximum = GetMp3Duration(currentSongPath).TotalSeconds;
			//Slider current time
			lblCurrenttime.Text = "0:00";
		}

		private void LoadSong()
		{
			isMusicLoaded = true;
			mediaPlayer.Source = new Uri(currentSongPath);
		}

		private ImageSource GetAlbumCoverImage()
		{
			string songDirectory = Path.GetDirectoryName(currentSongPath);
			string imagePath = Path.Combine(songDirectory, "cover.jpg") != null ?
				Path.Combine(songDirectory, "cover.jpg") :
				Path.Combine(songDirectory, "cover.png");

			currentImagePath = imagePath;

			if (File.Exists(currentImagePath))
			{
				BitmapImage bitmapImage = new BitmapImage();
				bitmapImage.BeginInit();
				bitmapImage.UriSource = new Uri(imagePath);
				bitmapImage.EndInit();

				return bitmapImage;
			}

			return null;
		}

		private void NavigateToSong(string direction)
		{
			if (songList.Count == 0)
			{
				return;
			}

			int factor = direction == "next" ? 1 : -1;

			currentSongIndex = (currentSongIndex + factor + songList.Count) % songList.Count;
			currentSongPath = songList[currentSongIndex];

			LoadSong();
			ViewSongInfo();

			playlistListBox.SelectedIndex = currentSongIndex;
			packIcon.Kind = PackIconKind.Pause;
			mediaPlayer.Play();
		}

		private void PlayPause()
		{
			if (!isMediaPlaying)
			{
				mediaPlayer.Play();
				isMediaPlaying = true;
				packIcon.Kind = PackIconKind.Pause;
			}
			else
			{
				mediaPlayer.Pause();
				packIcon.Kind = PackIconKind.Play;
				isMediaPlaying = false;
			}
		}
	}
}
