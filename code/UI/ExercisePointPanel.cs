﻿using Sandbox;
using Sandbox.UI;
using Sandbox.UI.Construct;
using System;

namespace TSS.UI
{
	public class ExercisePointPanel : WorldPanel
	{
		public float Opacity;
		public float FontSize;
		public Label l;
		public float TextScale;
		public TimeSince TimeSinceAlive;
		public float Life;
		public bool Fall;
		public Vector3 InitialPosition;
		public Vector3 Velocity;

		public string[] quips = new[]
		{
				"INSANE!",
				"KEEP IT UP!",
				"NICE WORK!",
				"AMAZING!",
				"GREAT!",
				"OUT OF THIS WORLD!",
				"WOW!",
				"AWESOME!",
				"FANTASTIC!",
				"LEGENDARY!",
				"NICE!",
				"OUTSTANDING!",
				"EXCELLENT!",
				"YOU ROCK!",
				"YOU ARE AMAZING!",
				"PERFECT!",
				"SUPER!",
				"MUSIC!",
				"GOOD JOB!",
		};

		public ExercisePointPanel( int points, int totalPoints )
		{
			Opacity = 1f;
			Fall = false;
			Life = Rand.Float( 1, 2f );
			TimeSinceAlive = 0;

			float width = 200;
			float height = 200;
			PanelBounds = new Rect( -(width / 2), -height, width, height );
			TextScale = 1f;
			string sign = (points > 0) ? "+" : " - ";
			string s = ((totalPoints + 1) % 25 == 0) ? quips[Rand.Int( 0, quips.Length - 1 )] : $"{sign}{points}";
			l = Add.Label( s, "title" );

			InitialPosition = Position;

			Velocity = Vector3.Up * Rand.Float( 40f, 50f ) + Vector3.Forward * Rand.Float( -30f, 30f ) + Vector3.Right * Rand.Float( -30f, 30f );

			StyleSheet.Load( "/ui/ExercisePointPanel.scss" );
		}

		public override void Tick()
		{
			base.Tick();

			if ( Local.Pawn is TSSPlayer pl )
			{
				if ( pl.CanGoToHeaven )
				{
					l.Style.FontColor = Color.Black;
				}
			}

			Style.Opacity = Opacity;
			if ( TimeSinceAlive > Life )
			{
				Delete( true );
				return;
			}

			Opacity = 1f - MathF.Pow( ((TimeSinceAlive - Life / 2f) / (Life / 2f)), 3f ).Clamp( 0, 1f );

			if ( !Fall )
			{
				Position = InitialPosition + Vector3.Up * 20f * MathF.Pow( TimeSinceAlive / Life, 2.0f );
				TextScale = TextScale.LerpTo( 1f, Time.Delta * 4f );
			}
			else
			{
				TextScale = TextScale.LerpTo( 1f, Time.Delta * 4f );
				Position = InitialPosition;
				InitialPosition += Velocity * Time.Delta;
				Velocity += Vector3.Down * 150f * Time.Delta;
			}

			Style.Dirty();
			if ( l != null )
			{
				l.Style.FontSize = Length.Pixels( FontSize * TextScale );
			}
			FontSize = 75f;

			if ( Local.Pawn is TSSPlayer player )
			{
				var cam = player.CameraMode as TSSCamera;
				Rotation = Rotation.LookAt( (cam.Position - Position) );
			}

		}
	}
}
