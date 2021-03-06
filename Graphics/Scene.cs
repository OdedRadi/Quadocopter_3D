﻿using OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Graphics
{
	class Scene
	{
		private const double m_maxFovyAngle = 90;
		private const double m_minFovyAngle = 45;
		private double m_fovyAngle = 75;

		private eThrottleStick m_throttleStickState = eThrottleStick.None;
		private eDirectionStick m_directionStickState = eDirectionStick.None;

		private float m_xTranslate;
		private float m_yTranslate;
		private float m_zTranslate;

		//private float m_xRotate;
		private float m_yRotate;
		//private float m_zRotate;

		private int m_width = 0;
		private int m_height = 0;

		private uint m_uint_DC = 0;
		private uint m_uint_RC = 0;

		private SkyBox m_skyBox = new SkyBox();
		private Helicopter m_helicopter = new Helicopter();
		private Skyscraper m_skyscraper = new Skyscraper();
		private Light m_light = new Light(0);

		public Scene()
		{
		}

		~Scene()
		{
			WGL.wglDeleteContext(m_uint_RC);
		}

		#region properties

		public bool LightSourceDrawing { private get; set; }

		public float LightPositionX
		{
			private get
			{
				return m_light.X;
			}
			set
			{
				m_light.X = value;
			}
		}

		public float LightPositionY
		{
			private get
			{
				return m_light.Y;
			}
			set
			{
				m_light.Y = value;
			}
		}

		public float LightPositionZ
		{
			private get
			{
				return m_light.Z;
			}
			set
			{
				m_light.Z = value;
			}
		}

		public float LookAtEyeX { private get; set; }
		public float LookAtEyeY { private get; set; }
		public float LookAtEyeZ { private get; set; }
		public float LookAtCenterX { private get; set; }
		public float LookAtCenterY { private get; set; }
		public float LookAtCenterZ { private get; set; }
		public float LookAtUpX { private get; set; }
		public float LookAtUpY { private get; set; }
		public float LookAtUpZ { private get; set; }

		#endregion

		#region init methods

		public void Init(Size sceneSize, uint sceneWindowId)
		{
			m_width = sceneSize.Width;
			m_height = sceneSize.Height;
			InitializeGL(sceneWindowId);
		}

		private void InitializeGL(uint sceneWindowId)
		{
			m_uint_DC = WGL.GetDC(sceneWindowId);
			WGL.wglSwapBuffers(m_uint_DC);

			initPixelFormat();
			initRenderingContext();
			initRenderingGL();
			initPerspective();

			m_xTranslate = -7;
			m_yTranslate = -3;
			m_zTranslate = -10;

			m_helicopter.Init();
			m_skyBox.Init();
			m_skyscraper.Init();
			m_light.Init();
		}

		private void initPixelFormat()
		{
			int pixelFormatIndex = 0;
			WGL.PIXELFORMATDESCRIPTOR pixelFormatDescriptor = new WGL.PIXELFORMATDESCRIPTOR();

			WGL.ZeroPixelDescriptor(ref pixelFormatDescriptor);
			pixelFormatDescriptor.nVersion = 1;
			pixelFormatDescriptor.dwFlags = (WGL.PFD_DRAW_TO_WINDOW | WGL.PFD_SUPPORT_OPENGL | WGL.PFD_DOUBLEBUFFER);
			pixelFormatDescriptor.iPixelType = (byte)(WGL.PFD_TYPE_RGBA);
			pixelFormatDescriptor.cColorBits = 32;
			pixelFormatDescriptor.cDepthBits = 32;
			pixelFormatDescriptor.iLayerType = (byte)(WGL.PFD_MAIN_PLANE);
			pixelFormatDescriptor.cStencilBits = 32;

			pixelFormatIndex = WGL.ChoosePixelFormat(m_uint_DC, ref pixelFormatDescriptor);
			if (pixelFormatIndex == 0)
			{
				MessageBox.Show("Unable to retrieve pixel format");
				return;
			}

			if (WGL.SetPixelFormat(m_uint_DC, pixelFormatIndex, ref pixelFormatDescriptor) == 0)
			{
				MessageBox.Show("Unable to set pixel format");
				return;
			}
		}

		private void initRenderingContext()
		{
			m_uint_RC = WGL.wglCreateContext(m_uint_DC);

			if (m_uint_RC == 0)
			{
				MessageBox.Show("Unable to get rendering context");
				return;
			}
			if (WGL.wglMakeCurrent(m_uint_DC, m_uint_RC) == 0)
			{
				MessageBox.Show("Unable to make rendering context current");
				return;
			}
		}

		private void initRenderingGL()
		{
			if (m_uint_DC == 0 || m_uint_RC == 0)
				return;

			GL.glEnable(GL.GL_DEPTH_TEST);
			GL.glDepthFunc(GL.GL_LEQUAL);
			GL.glViewport(0, 0, m_width, m_height);
			GL.glClearColor(0, 0, 0, 0);
		}

		private void initPerspective()
		{
			GL.glMatrixMode(GL.GL_PROJECTION);
			GL.glLoadIdentity();

			GLU.gluPerspective(m_fovyAngle, m_width / m_height, 1, 3000);

			GL.glMatrixMode(GL.GL_MODELVIEW);
			GL.glLoadIdentity();
		}
		#endregion

		public void Draw()
		{
			if (m_uint_DC == 0 || m_uint_RC == 0)
				return;

			GL.glClear(GL.GL_COLOR_BUFFER_BIT | GL.GL_DEPTH_BUFFER_BIT | GL.GL_STENCIL_BUFFER_BIT);
			GL.glLoadIdentity();
			GL.glTranslated(0, 0, -5);

			GLU.gluLookAt(
				LookAtEyeX, LookAtEyeY, LookAtEyeZ, 
				LookAtCenterX, LookAtCenterY, LookAtCenterZ, 
				LookAtUpX, LookAtUpY, LookAtUpZ);

			checkSticksState();
			enableLightning();
			drawReflectedScene();

			// the world is moving (the helicopter always on 0,0,0), its looks like the helicopter is moving
			GL.glRotatef(m_yRotate, 0, 1, 0);
			GL.glTranslatef(m_xTranslate, m_yTranslate, m_zTranslate);

			m_skyscraper.Draw();

			// the skybox always keep the same distance from the helicopter, so it is not affected by translation
			GL.glTranslatef(-m_xTranslate, -m_yTranslate, -m_zTranslate);
			m_skyBox.Draw();

			// the helicopter is not affected by translation or rotation (first person view)
			GL.glRotatef(m_yRotate, 0, -1, 0);
			m_helicopter.Draw();

			drawShadows();

			GL.glFlush();
			WGL.wglSwapBuffers(m_uint_DC);
		}

		private void enableLightning()
		{
			GL.glPushMatrix();

			GL.glRotatef(m_yRotate, 0, 1, 0);
			m_light.SetEnable(true);

			if (LightSourceDrawing)
			{
				GL.glTranslatef(m_xTranslate, m_yTranslate, m_zTranslate);
				m_light.DrawLightSource();
			}

			GL.glPopMatrix();
		}

		private void drawReflectedScene()
		{
			GL.glPushMatrix();

			// setting light source Z position negetive, because reflected scene is drawing negetive at Z axis
			m_light.Z *= -1;
			m_light.SetEnable(true);
			m_light.Z *= -1; // correct the Z position for correct reflected shadow drawing

			// move to skyscraper origins
			GL.glRotatef(m_yRotate, 0, 1, 0);
			GL.glTranslatef(m_xTranslate, m_yTranslate, m_zTranslate);

			m_skyscraper.DrawReflection(m_light.Position);

			drawSkyboxReflection();

			if (m_zTranslate < -1)
			{
				drawHelicopterReflection();
			}
			
			// after finished drawing, activate the original light position again
			m_light.SetEnable(true);

			GL.glPopMatrix();
		}

		private void drawHelicopterReflection()
		{
			// set helicopter drawing trasnlation and rotating
			float[] drawingTranslation = new float[3] { -m_xTranslate, -m_yTranslate, -m_zTranslate };
			float[] drawingRotation = new float[3] { 0, -m_yRotate, 0 };

			m_skyscraper.DrawComponentReflection(m_helicopter, drawingTranslation, drawingRotation, m_light.Position);	
		}

		private void drawSkyboxReflection()
		{
			// set skybox drawing trasnlation and rotating
			float[] drawingTranslation = new float[3] { -m_xTranslate, -m_yTranslate, -m_zTranslate };
			float[] drawingRotation = new float[3] { 0, 0, 0 }; // skybox no affected by rotation

			m_skyscraper.DrawComponentReflection(m_skyBox, drawingTranslation, drawingRotation, m_light.Position);
		}

		private void drawShadows()
		{
			GL.glPushMatrix();

			// move to skyscraper origins
			GL.glRotatef(m_yRotate, 0, 1, 0);
			GL.glTranslatef(m_xTranslate, m_yTranslate, m_zTranslate);

			// get helicopter drawing trasnlation
			float[] drawingTranslation = new float[3];

			drawingTranslation[0] = -m_xTranslate;
			drawingTranslation[1] = -m_yTranslate;
			drawingTranslation[2] = -m_zTranslate;

			// get helicopter drawing rotating
			float[] drawingRotation = new float[3];

			drawingRotation[0] = 0;
			drawingRotation[1] = -m_yRotate;
			drawingRotation[2] = 0;

			m_skyscraper.DrawComponentShadows(m_light.Position, m_helicopter, drawingTranslation, drawingRotation);
			
			GL.glPopMatrix();
		}

		public void ChangeLookDistance(eLookDistance lookDistance)
		{
			switch (lookDistance)
			{
				case eLookDistance.Further:
					if (m_fovyAngle < m_maxFovyAngle)
					{
						m_fovyAngle++;
					}
					break;
				case eLookDistance.Closer:
					if (m_fovyAngle > m_minFovyAngle)
					{
						m_fovyAngle--;
					}
					break;
			}

			initPerspective();
		}

		#region sticks handlers
		public void ThrottleStickActivate(eThrottleStick throttleStickState)
		{
			m_helicopter.ThrottleStickState |= throttleStickState;
			m_throttleStickState |= throttleStickState;
		}

		public void ThrottleStickDeactivate(eThrottleStick throttleStickState)
		{
			m_helicopter.ThrottleStickState &= ~throttleStickState;
			m_throttleStickState &= ~throttleStickState;
		}

		public void DirectionStickActivate(eDirectionStick directionStickState)
		{
			m_helicopter.DirectionStickState |= directionStickState;
			m_directionStickState |= directionStickState;
		}

		public void DirectionStickDeactivate(eDirectionStick directionStickState)
		{
			m_helicopter.DirectionStickState &= ~directionStickState;
			m_directionStickState &= ~directionStickState;
		}

		private void checkSticksState()
		{
			checkThrottleStickState();
			checkDirectionStickState();
		}

		private void checkThrottleStickState()
		{
			if (m_throttleStickState.HasFlag(eThrottleStick.Ascend))
			{
				m_yTranslate -= 0.2f;
			}
			if (m_throttleStickState.HasFlag(eThrottleStick.Descend))
			{
				m_yTranslate += 0.2f;
			}
			if (m_throttleStickState.HasFlag(eThrottleStick.Right))
			{
				m_yRotate += 1;
			}
			if (m_throttleStickState.HasFlag(eThrottleStick.Left))
			{
				m_yRotate -= 1;
			}
		}

		private void checkDirectionStickState()
		{
			if (m_directionStickState.HasFlag(eDirectionStick.Forward))
			{
				m_xTranslate -= (float)Math.Sin((Math.PI / 180) * m_yRotate) * 0.2f;
				m_zTranslate += (float)Math.Cos((Math.PI / 180) * m_yRotate) * 0.2f;
			}
			if (m_directionStickState.HasFlag(eDirectionStick.Backward))
			{
				m_xTranslate += (float)Math.Sin((Math.PI / 180) * m_yRotate) * 0.2f;
				m_zTranslate -= (float)Math.Cos((Math.PI / 180) * m_yRotate) * 0.2f;
			}
			if (m_directionStickState.HasFlag(eDirectionStick.Right))
			{
				m_xTranslate -= (float)Math.Cos((Math.PI / 180) * m_yRotate) * 0.2f;
				m_zTranslate -= (float)Math.Sin((Math.PI / 180) * m_yRotate) * 0.2f;
			}
			if (m_directionStickState.HasFlag(eDirectionStick.Left))
			{
				m_xTranslate += (float)Math.Cos((Math.PI / 180) * m_yRotate) * 0.2f;
				m_zTranslate += (float)Math.Sin((Math.PI / 180) * m_yRotate) * 0.2f;
			}
		}
		#endregion
		
		public void drawAxes()
		{
			GL.glBegin(GL.GL_LINES);
			//x  RED
			GL.glColor3f(1.0f, 0.0f, 0.0f);
			GL.glVertex3f(0.0f, 0.0f, 0.0f);
			GL.glVertex3f(300.0f, 0.0f, 0.0f);
			//y  GREEN 
			GL.glColor3f(0.0f, 1.0f, 0.0f);
			GL.glVertex3f(0.0f, 0.0f, 0.0f);
			GL.glVertex3f(0.0f, 300.0f, 0.0f);
			//z  BLUE
			GL.glColor3f(0.0f, 0.0f, 1.0f);
			GL.glVertex3f(0.0f, 0.0f, 0.0f);
			GL.glVertex3f(0.0f, 0.0f, 300.0f);

			GL.glEnd();
		}
	}
}
