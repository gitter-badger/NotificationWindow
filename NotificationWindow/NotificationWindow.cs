﻿using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Threading;
using System;
using System.Timers;
using System.Windows.Forms;
using NotificationWindow;

namespace RemoteWindowsAdministrator {
	public partial class NotificationWindow: Form {
		internal class NotificationMessage {
			public string Message {
				get;
				private set;
			}

			private readonly DateTime _timestamp;
			public DateTime Timestamp { get { return _timestamp; } }

			public NotificationMessage( string message ) {
				Message = message;
				_timestamp = DateTime.Now;
			}
		}

		public static void AddMessage( string format, params object[] values ) {
			var message = string.Format( format, values );
			if( null == _window ) {
				_window = new NotificationWindow( );
				_window.Show( );
			}
			lock( _lock ) {
				_messages.Add( new NotificationMessage( message ) );
			}
		}

		private NotificationWindow( ) {
			InitializeComponent( );
			this.FormBorderStyle = FormBorderStyle.None;
			dgvMessages.AllowUserToAddRows = false;
			dgvMessages.AllowUserToDeleteRows = false;
			dgvMessages.AllowUserToOrderColumns = false;
			dgvMessages.AllowUserToResizeColumns = false;
			dgvMessages.AllowUserToResizeRows = false;
			dgvMessages.RowHeadersVisible = false;
			dgvMessages.ColumnHeadersVisible = false;
			dgvMessages.AutoGenerateColumns = false;
			dgvMessages.Columns.Add( new DataGridViewColumn {Name = @"Message", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill} );
			_messages = new BindingList<NotificationMessage>( );

			dgvMessages.Click += delegate( object sender, EventArgs e ) {
				InvokeIfNeeded( ( ) => {
					CloseForm( 1000 );
				} );
			};

			dgvMessages.DataSource = _messages;
			_timer = new System.Timers.Timer( 1000 );
			_timer.Elapsed += ShouldIStayOpen;
			_timer.Enabled = true;
		}

		private void NotificationWindow_Load( object sender, EventArgs e ) {
			SetRight( Screen.GetBounds( this ).Right );
			SetBottom( Screen.GetWorkingArea( this ).Bottom );
		}


		private static BindingList<NotificationMessage> _messages;
		private static NotificationWindow _window;
		private System.Timers.Timer _timer;
		private static string _lock = @"IAMLOCKED";


		private static int CleanMessages( int maxAgeSeconds ) {
			if( null == _messages ) {
				return 0;
			}
			lock( _lock ) {
				var now = DateTime.Now;
				_messages.RemoveAll( message => (now - message.Timestamp).TotalSeconds >= maxAgeSeconds );
				return _messages.Count;
			}
		}

// 		public static void RemoveAll<T>( BindingList<T> values, Predicate<T> predicate ) {
// 			var itemsToRemove = new List<int>( );
// 			for( var n = values.Count - 1; n >= 0; --n ) {
// 				if( predicate( values[n] ) ) {
// 					values.RemoveAt( n );
// 				}
// 			}
// 
// 		}
// 
		private void ShouldIStayOpen( Object source, ElapsedEventArgs e ) {
			var messagesLeft = CleanMessages( 3 );
			if( 0 != messagesLeft || null == _window ) {
				_timer.Enabled = true;
				return;
			}
			_window.CloseForm( );
			_window = null;
		}

		private void InvokeIfNeeded( Action action ) {
			if( InvokeRequired ) {
				Invoke( action );
			} else {
				action( );
			}
		}

		private void SetHeight( int position ) {
			Helpers.Assert( 0 <= position, @"Heights must be at least 0" );
			Helpers.Assert( Screen.GetWorkingArea( this ).Height >= position, "Height is out of bounds" );
			InvokeIfNeeded( ( ) => { Height = position; } );
		}

		private void SetRight( int position ) {
			Helpers.Assert( 0 <= position, @"Right must be at least 0" );
			Helpers.Assert( Screen.GetWorkingArea( this ).Right >= position, "Right is out of bounds" );
			
			InvokeIfNeeded( ( ) => {
				Left = position - Width;
			} );
		}

		private void SetBottom( int position ) {
			Helpers.Assert( 0 <= position, @"Bottom must be at least 0" );
			Helpers.Assert( Screen.GetWorkingArea( this ).Bottom >= position, "Bottom is out of bounds" );

			InvokeIfNeeded( ( ) => {
				Top = position - Height;
			} );
		}

		private void SetDockState( Control ctrl, bool isDocked ) {
			if( null != ctrl && !ctrl.IsDisposed ) {
				InvokeIfNeeded( ( ) => { ctrl.Dock = isDocked ? DockStyle.Fill : DockStyle.None; } );
			}
		}

		private void SetVisible( Control ctrl, bool isVisible ) {
			if( null != ctrl && !ctrl.IsDisposed ) {
				InvokeIfNeeded( ( ) => { ctrl.Visible = isVisible; } );
			}
		}

		private void SetOpacity( double percentOpac ) {
			InvokeIfNeeded( ( ) => { Opacity = percentOpac; } );
		}

		private void FadeAway( int millisecondsToWork ) {
			var timeBetweenFrames = (int)(millisecondsToWork / 100.0);
			while( Opacity >= 0 ) {
				SetOpacity( Opacity - 0.01 );
				Thread.Sleep( timeBetweenFrames );
				timeBetweenFrames = timeBetweenFrames % 2 == 0 ? timeBetweenFrames - 1 : timeBetweenFrames;
			}
		}

		private void CloseForm( int millisecondsToClose = 0 ) {
			new Thread( ( ) => {
				lock( _lock ) {
					if( null != _messages && 0 < _messages.Count ) {
						_messages.Clear( );
						_messages = null;
					}
				}
				FadeAway( millisecondsToClose );
				//Shrink( 5, 5, true );
				InvokeIfNeeded( ( ) => {
					Close( );
					Dispose( );
				} );
			} ).Start( );
		}
	}



}