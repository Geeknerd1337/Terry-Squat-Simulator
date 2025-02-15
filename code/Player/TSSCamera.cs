﻿using Sandbox;
using System;
using System.Linq;
using TSS.UI;

public enum CameraState
{
	Static,
	Follow,
	Topdown,
	Beat,
	Ground,
	Intro,
	Ending
};


namespace TSS
{
	public partial class TSSCamera : CameraMode
	{
		public float IntroCamHeight;
		public float CamDistance;
		public Vector3 CamOffset;
		public CameraState CamState;
		public float Progress;
		private float TimedProgress;
		float yaw;
		float yawTar;
		public bool IntroComplete;
		public float TimeSinceState;
		public bool SkipIntro;

		//Credit Panels (Definitely a better way to do this but this is just for the intro)
		private CreditPanel JoshWilson;
		private CreditPanel Presents;
		private CreditPanel Assoc;
		private CreditPanel Dawdle;
		private CreditPanel Mungus;
		private CreditPanel Jacob;
		private CreditPanel Kabubu;
		private CreditPanel TSS;
		public CreditPanel Up;
		public CreditPanel Down;
		public CreditPanel TreadmillTutorial;
		public CreditPanel SCounter;
		public TimeSince TimeSinceStart;
		public bool RunTutorial;
		public bool RunTutorialComplete;

		public bool Active;

		private float EndingBump;


		public override void Activated()
		{
			base.Activated();
			if ( !Active )
			{
				if ( !SkipIntro )
				{
					CamState = CameraState.Intro;
					IntroComplete = false;
					JoshWilson = null;
					TimeSinceStart = 0;
					Active = true;
				}
				else
				{
					var pawn = Local.Pawn as TSSPlayer;
					IntroComplete = true;
					CamState = CameraState.Static;
					Progress = 0f;
					TimeSinceState = 0f;
					SCounter ??= new CreditPanel( "Squats: 0", 3200, 3200 );
					SCounter.Position = pawn.ExercisePosition + Vector3.Up * 30f + pawn.Rotation.Forward * -50f;
					SCounter.Rotation = Rotation.From( 0, 90, 0 );
					SCounter.Opacity = 0.0f;
					SCounter.TextScale = 1.0f;
					Active = true;
				}
			}
		}

		public override void Update()
		{
			var pawn = Local.Pawn as TSSPlayer;
			yaw = yaw.LerpTo( yawTar, Time.Delta * 2f );

			if ( pawn.CurrentExercise != Exercise.Squat )
			{
				Progress = 1.0f;
			}

			//If we're in the final portion of the game, set the camera state to static
			if ( pawn.CanGoToHeaven )
			{
				CamState = CameraState.Ending;
			}

			switch ( CamState )
			{
				case CameraState.Follow:
					FollowPlayer();
					break;
				case CameraState.Intro:
					AdvanceIntro();
					break;
				case CameraState.Ground:
					Ground();
					break;
				case CameraState.Static:
					StaticPlayer();
					break;
				case CameraState.Topdown:
					Topdown();
					break;
				case CameraState.Beat:
					Beat();
					break;
				case CameraState.Ending:
					Ending();
					break;
			}

			if ( pawn.GetAnimParameterBool( "Drink" ) && pawn.TimeSinceSoda > 0.05f )
			{
				var transform = pawn.GetBoneTransform( "Camera" );
				Position = transform.Position;
				Rotation = transform.Rotation * Rotation.From( 90, 0, -90 );
			}

			if ( pawn.GetAnimParameterBool( "Ending" ) && pawn.TimeSinceEnding > 0.02f )
			{
				var transform = pawn.GetBoneTransform( "Camera" );
				Position = transform.Position;
				Rotation = transform.Rotation * Rotation.From( 90, 0, -90 );
			}

			Progress = Math.Clamp( Progress, 0f, 1f );
			float f = (pawn.TimeSinceExerciseStopped - 1f) / 3f;
			f = MathF.Pow( f.Clamp( 0, 1f ), 3f );
			TimeSinceState += Time.Delta * (1f - f);
			TimedProgress = TimedProgress.LerpTo( TimeSinceState / 5f, Time.Delta * 8f );
			TimedProgress = TimedProgress.Clamp( 0f, 1f );

			if ( TSS != null && IntroComplete && pawn.CurrentExercise != Exercise.Run )
			{
				TSS.Opacity -= Time.Delta * pawn.CurrentExerciseSpeed * 0.5f;

				if ( TSS.Opacity <= 0f )
				{
					TSS?.Delete();
					TSS = null;
				}

				Down?.Delete();
				Down = null;
				Up?.Delete();
				Up = null;
			}

			#region Run Tutorial Prompt
			//Spawn a tutorial prompt for running

			if ( !RunTutorial && pawn.CurrentExercise == Exercise.Run )
			{
				Down?.Delete();
				Up?.Delete();
				Down = null;
				Up = null;
				Up ??= new CreditPanel( Input.GetKeyWithBinding( "+iv_right" ).ToUpper(), 200, 200 );
				Down ??= new CreditPanel( Input.GetKeyWithBinding( "+iv_left" ).ToUpper(), 200, 200 );


				RunTutorial = true;
			}


			//Basically fade these out after a few seconds.
			if ( RunTutorial && !RunTutorialComplete )
			{
				float runTutAlph = 0f;

				var component = pawn.Components.GetAll<RunComponent>().First();

				if ( component.TimeSinceRun > 8f )
				{
					runTutAlph = ((component.TimeSinceRun - 8f) / 3f).Clamp( 0, 1f );
				}

				if ( Up != null && Down != null )
				{
					Down.TextScale = Down.TextScale.LerpTo( 1, Time.Delta * 10f );
					Up.TextScale = Up.TextScale.LerpTo( 1, Time.Delta * 10f );

					Up.Position = pawn.ExercisePosition + Vector3.Up * 55f + pawn.Rotation.Right * -22f;
					Up.Rotation = pawn.Rotation;
					Up.Opacity = 1 - runTutAlph;

					Down.Position = pawn.ExercisePosition + Vector3.Up * 55f + pawn.Rotation.Right * 22f;
					Down.Rotation = pawn.Rotation;
					Down.Opacity = 1 - runTutAlph;

					if ( component.TimeSinceRun > 15f )
					{
						RunTutorialComplete = true;
						Down?.Delete();
						Up?.Delete();
						Down = null;
						Up = null;
					}
				}
			}
			#endregion

			//For now make the score face in the forward direction of the player
			if ( Local.Pawn is TSSPlayer t )
			{
				if ( SCounter != null )
				{
					SCounter.Rotation = t.Rotation;
					SCounter.Position = t.ExercisePosition + Vector3.Up * 30f + pawn.Rotation.Forward * -50f;
				}
			}
		}

		public void AdvanceIntro()
		{

			var pawn = Local.Pawn as TSSPlayer;

			if ( Progress > 0.01f )
			{
				TSSGame.Current.SetTarVolume( 1 );
				TSSGame.Current.SetTarVolume( 0 );

			}

			if ( Progress < 0.25f )
			{

				JoshWilson ??= new CreditPanel( "Josh Wilson", 3200, 3200 );
				JoshWilson.Position = pawn.ExercisePosition + Vector3.Up * 10f + pawn.Rotation.Forward * -20f;
				JoshWilson.Rotation = Rotation.From( 0, 90, 0 );
				JoshWilson.Opacity = ((Progress - 0.01f) / 0.05f).Clamp( 0, 1f );
				JoshWilson.Bop = true;

				Presents ??= new CreditPanel( "Presents", 3200, 3200 );
				Presents.Position = pawn.ExercisePosition + Vector3.Up * -50f + pawn.Rotation.Forward * 9f;
				Presents.Rotation = Rotation.From( 0, 90, 0 );
				Presents.Opacity = ((Progress - 0.1f) / 0.05f).Clamp( 0, 1f );
				Presents.Bop = true;

				float f = ((TimeSinceStart - 2f) / 5f).Clamp( 0, 1f );

				Up ??= new CreditPanel( Input.GetKeyWithBinding( "+iv_forward" ), 200, 200 );
				Up.Position = pawn.ExercisePosition + Vector3.Up * 55f + pawn.Rotation.Right * -22f + pawn.Rotation.Forward * 12f;
				Up.Rotation = Rotation.From( 0, 90, 0 );
				Up.Opacity = (1f - ((Progress - 0.1f) / 0.05f).Clamp( 0, 1f )) * f;
				Up.TextScale = Up.TextScale.LerpTo( 1, Time.Delta * 10f );

				Down ??= new CreditPanel( Input.GetKeyWithBinding( "+iv_back" ), 200, 200 );
				Down.Position = pawn.ExercisePosition + Vector3.Up * 25f + pawn.Rotation.Right * -22f + pawn.Rotation.Forward * 12f;
				Down.Rotation = Rotation.From( 0, 90, 0 );
				Down.Opacity = (1f - ((Progress - 0.1f) / 0.05f).Clamp( 0, 1f )) * f;
				Down.TextScale = Down.TextScale.LerpTo( 1, Time.Delta * 10f );

				var center = pawn.ExercisePosition + Vector3.Up * IntroCamHeight;

				CamDistance = 125f - 50f * (Progress / 0.25f);
				IntroCamHeight = 45f;
				Position = center + pawn.Rotation.Forward * CamDistance;
				Rotation = Rotation.LookAt( (center - Position), Vector3.Up );
				yaw = 30f;
				yawTar = 30f;
			}

			if ( Progress >= 0.25f )
			{
				JoshWilson?.Delete();
				JoshWilson = null;
				Presents?.Delete();
				Presents = null;
			}

			if ( Progress >= 0.25f && Progress < 0.5f )
			{

				Assoc ??= new CreditPanel( "", 350, 350, "materials/ui/eo_logo.png" );
				Assoc.Rotation = Rotation.From( 0, 90, 0 );
				Assoc.Position = pawn.ExercisePosition + pawn.Rotation.Forward * 15f + Vector3.Up * 24f;
				Assoc.Opacity = 1f;
				Assoc.Bop = true;
				Assoc.FontSize = 100f;

				Dawdle ??= new CreditPanel( "Dawdle", 3200, 400 );
				Dawdle.Rotation = Rotation.From( 0, 55, 0 );
				Dawdle.Position = pawn.ExercisePosition + pawn.Rotation.Right * -50f + Vector3.Up * -3f;
				Dawdle.Opacity = 1f;
				Dawdle.FontSize = 200f;

				Mungus ??= new CreditPanel( "Mungus", 3200, 400 );
				Mungus.Rotation = Rotation.From( 0, -55 + 180, 0 );
				Mungus.Position = pawn.ExercisePosition + pawn.Rotation.Right * 50f + Vector3.Up * -3f;
				Mungus.Opacity = 1f;
				Mungus.FontSize = 200f;

				Kabubu ??= new CreditPanel( "Kabubu", 3200, 400 );
				Kabubu.Rotation = Rotation.From( 0, 55, 0 );
				Kabubu.Position = pawn.ExercisePosition + pawn.Rotation.Right * -50f + Vector3.Up * 64f;
				Kabubu.Opacity = 1f;
				Kabubu.FontSize = 200f;

				Jacob ??= new CreditPanel( "Jac0xb", 3200, 400 );
				Jacob.Rotation = Rotation.From( 0, -55 + 180, 0 );
				Jacob.Position = pawn.ExercisePosition + pawn.Rotation.Right * 50f + Vector3.Up * 64f;
				Jacob.Opacity = 1f;
				Jacob.FontSize = 200f;

				float p = (Progress - 0.25f) / 0.24f;
				p = p.Clamp( 0, 1f );
				CamDistance = 150f;
				IntroCamHeight = 20f;
				var center = pawn.ExercisePosition + Vector3.Up * IntroCamHeight;

				yawTar = MathX.LerpTo( 30f, 120f, p );
				Position = center + Rotation.FromYaw( yaw ).Forward * CamDistance;
				Rotation = Rotation.LookAt( center - Position, Vector3.Up );

				//TSSGame.Current.SetSingleTarVolume( 1 );
				TSSGame.Current.SetTarVolume( 2 );
				TSSGame.Current.SetTarVolume( 3 );
			}

			if ( Progress >= 0.5f )
			{
				Assoc?.Delete();
				Assoc = null;
				Dawdle?.Delete();
				Dawdle = null;
				Mungus?.Delete();
				Mungus = null;
				Kabubu?.Delete();
				Kabubu = null;
				Jacob?.Delete();
				Jacob = null;
				Up?.Delete();
				Up = null;
				Down?.Delete();
				Up = null;
			}

			if ( Progress >= 0.5f && Progress < 0.75f )
			{
				float p = (Progress - 0.5f) / 0.25f;
				p = p.Clamp( 0, 1f );
				CamDistance = 50f;
				IntroCamHeight = 32f + 32f * p;

				var center = pawn.ExercisePosition + Vector3.Up * IntroCamHeight;
				Position = center + pawn.Rotation.Forward * CamDistance;
				Rotation = Rotation.LookAt( (center - Position), Vector3.Up );

				TSSGame.Current.SetTarVolume( 5 );


			}

			if ( Progress >= 0.75f && Progress <= 1f )
			{
				float p = (Progress - 0.75f) / 0.24f;
				p = p.Clamp( 0, 1f );
				CamDistance = 50f + 50f * p;
				IntroCamHeight = 64f - 19f * p;

				TSS ??= new CreditPanel( "Terry\nSquat\nSimulator", 3200, 3200 );
				TSS.Position = pawn.ExercisePosition + Vector3.Up * -26f + pawn.Rotation.Forward * 20f;
				TSS.Rotation = Rotation.From( 0, 90, 0 );
				TSS.Opacity = p * 2f;
				TSS.Bop = true;

				//TSSGame.Current.SetSingleTarVolume( 2 );
				TSSGame.Current.SetTarVolume( 4 );


				var center = pawn.ExercisePosition + Vector3.Up * IntroCamHeight;
				Rotation = Rotation.LookAt( (center - Position), Vector3.Up );
				Position = center + pawn.Rotation.Forward * CamDistance;
			}

			if ( Progress >= 1.0f )
			{
				IntroComplete = true;
				CamState = CameraState.Static;
				Progress = 0f;
				TimeSinceState = 0f;

				if ( TSS != null )
				{
					TSS.Opacity = 1f;
					TSS?.Delete();
					TSS = null;
				}

				TSSGame.Current.SetTarVolume( 6 );

				SCounter ??= new CreditPanel( "Squats: 0", 3200, 3200 );
				SCounter.Position = pawn.ExercisePosition + Vector3.Up * 30f + pawn.Rotation.Forward * -50f;
				SCounter.Rotation = Rotation.From( 0, 90, 0 );
				SCounter.Opacity = 0.0f;
				SCounter.TextScale = 1.0f;
			}
		}

		public void FollowPlayer()
		{
			CamDistance = 125f;
			float p = TimedProgress;
			p = p.Clamp( 0, 1f );
			var pawn = Local.Pawn as TSSPlayer;
			var center = GetCenter();


			Position = center + pawn.Rotation.Forward * CamDistance + Vector3.Up * GetSceneCameraHeight() + pawn.Rotation.Right * MathX.LerpTo( -100f, 100f, p );
			Rotation = Rotation.LookAt( (center - Position), Vector3.Up );

			if ( TimeSinceState > 5f )
			{
				NextCameraScene();
			}
		}


		public void Ground()
		{
			CamDistance = 125f;
			float p = TimedProgress;
			p = p.Clamp( 0, 1f );
			var pawn = Local.Pawn as TSSPlayer;
			var center = GetCenter();

			Position = center + pawn.Rotation.Forward * CamDistance + Vector3.Up * GetSceneCameraHeight() + pawn.Rotation.Right * MathX.LerpTo( -50f, 50f, p );
			Rotation = Rotation.LookAt( pawn.Rotation.Forward * -1f, Vector3.Up );

			if ( TimeSinceState > 5f )
			{
				NextCameraScene();
			}

		}

		public void StaticPlayer()
		{
			CamDistance = 100f;
			var pawn = Local.Pawn as TSSPlayer;
			var center = GetCenter();

			Position = center + pawn.Rotation.Forward * CamDistance + Vector3.Up * GetSceneCameraHeight();
			Rotation = Rotation.LookAt( (center - Position), Vector3.Up );

			if ( TimeSinceState > 5f )
			{
				NextCameraScene();
			}

		}

		[Event( "OtherBeat" )]
		public void EndBump()
		{
			EndingBump = 20f;
		}

		public void Ending()
		{
			EndingBump = EndingBump.LerpTo( 0f, Time.Delta );
			CamDistance = 100f + EndingBump;
			var pawn = Local.Pawn as TSSPlayer;
			var center = GetCenter();

			Position = center + pawn.Rotation.Forward * CamDistance + Vector3.Up * GetSceneCameraHeight();
			Rotation = Rotation.LookAt( (center - Position), Vector3.Up ) * Rotation.FromAxis( pawn.Rotation.Right, 35f * MathF.Sin( Time.Now ) );

			if ( TimeSinceState > 5f )
			{
				NextCameraScene();
			}

		}

		public void Topdown()
		{
			CamDistance = 100f;
			var pawn = Local.Pawn as TSSPlayer;
			var center = GetCenter();

			Position = center + pawn.Rotation.Up * CamDistance + new Vector3( 32 * MathF.Sin( Time.Now / 3 ), 32 * MathF.Cos( Time.Now / 3 ), 0 );
			var hitPos = Trace.Ray( pawn.Position + Vector3.Up * 10f, Position ).Ignore( pawn ).WithoutTags( "wall" );
			Position = hitPos.Run().EndPosition;

			Rotation = Rotation.LookAt( (center - Position), Vector3.Up );

			if ( pawn.CurrentExercise == Exercise.Yoga || TimeSinceState > 5f )
			{
				NextCameraScene();
			}

		}

		public void Beat()
		{
			CamDistance = 100f;
			var pawn = Local.Pawn as TSSPlayer;
			var center = GetCenter();

			var beatMultiplier = 2f;

			if ( pawn.CurrentExercise == Exercise.Punch )
			{
				beatMultiplier = 1 / 4f;
			}

			var beatFreq = MathF.PI / 4 * TSSGame.Current.BeatNonce * beatMultiplier;

			Position = center + pawn.Rotation.Forward * 128f + Vector3.Up * GetSceneCameraHeight() + new Vector3( 32 * MathF.Sin( beatFreq ), 32 * MathF.Cos( beatFreq ), 0 );
			Rotation = Rotation.LookAt( (center - Position), Vector3.Up );


			if ( TimeSinceState > 10f )
			{
				NextCameraScene();
			}
		}

		public void NextCameraScene()
		{
			var states = new CameraState[] {
				CameraState.Static,
				CameraState.Follow,
				CameraState.Topdown,
				CameraState.Beat,
				CameraState.Ground
			};

			TimeSinceState = 0f;
			TimedProgress = 0f;
			CamState = states[((int)CamState + 1) % states.Length];
		}

		public Vector3 GetCenter()
		{
			var pawn = Local.Pawn as TSSPlayer;
			return pawn.ExercisePosition + Vector3.Up * 45f;
		}

		public float GetSceneCameraHeight()
		{
			var pawn = Local.Pawn as TSSPlayer;


			if ( CamState == CameraState.Ground )
			{
				return 15f;
			}
			else if ( pawn.CurrentExercise == Exercise.Run )
			{
				return 45f;
			}
			else
			{
				return 0f;

			}
		}

	}
}
