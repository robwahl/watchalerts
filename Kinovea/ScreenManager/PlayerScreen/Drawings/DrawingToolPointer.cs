/*
Copyright © Joan Charmant 2008.
joan.charmant@gmail.com

This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.

*/

using Kinovea.ScreenManager.Languages;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace Kinovea.ScreenManager
{
    public class DrawingToolPointer : AbstractDrawingTool
    {
        #region Constructor

        public DrawingToolPointer()
        {
            _mUserAction = UserAction.None;
            _mSelectedObjectType = SelectedObjectType.None;
            _mLastPoint = new Point(0, 0);
            _mIResizingHandle = 0;
            _mImgSize = new Size(320, 240);
            MouseDelta = new Point(0, 0);
            _mDirectZoomTopLeft = new Point(-1, -1);

            SetupHandCursors();
        }

        #endregion Constructor

        #region Enum

        private enum UserAction
        {
            None,
            Move,
            Resize
        }

        private enum SelectedObjectType
        {
            None,
            Track,
            Chrono,
            Drawing,
            ExtraDrawing,
            Grid,
            Plane
        }

        #endregion Enum

        #region Properties

        public override string DisplayName
        {
            get { return ScreenManagerLang.ToolTip_DrawingToolPointer; }
        }

        public override Bitmap Icon
        {
            get { return Properties.Drawings.handtool; }
        }

        public override bool Attached
        {
            get { return false; }
        }

        public override bool KeepTool
        {
            get { return true; }
        }

        public override bool KeepToolFrameChanged
        {
            get { return true; }
        }

        public override DrawingStyle StylePreset
        {
            get { throw new NotImplementedException(); }
            set { throw new NotImplementedException(); }
        }

        public override DrawingStyle DefaultStylePreset
        {
            get { throw new NotImplementedException(); }
        }

        public Point MouseDelta { get; private set; }

        #endregion Properties

        #region Members

        //--------------------------------------------------------------------
        // We do not keep the strecth/zoom factor here.
        // All coordinates must be given already descaled to image coordinates.
        //--------------------------------------------------------------------
        private UserAction _mUserAction;

        private SelectedObjectType _mSelectedObjectType;
        private Point _mLastPoint;
        private Point _mDirectZoomTopLeft;
        private int _mIResizingHandle;
        private Size _mImgSize;
        private Cursor _mCurHandOpen;
        private Cursor _mCurHandClose;
        private int _mILastCursorType;

        #endregion Members

        #region AbstractDrawingTool Implementation

        public override AbstractDrawing GetNewDrawing(Point origin, long iTimestamp, long averageTimeStampsPerFrame)
        {
            return null;
        }

        public override Cursor GetCursor(double fStretchFactor)
        {
            throw new NotImplementedException();
        }

        #endregion AbstractDrawingTool Implementation

        #region Public Interface

        public void OnMouseUp()
        {
            _mUserAction = UserAction.None;
        }

        public bool OnMouseDown(Metadata metadata, int iActiveKeyFrameIndex, Point mouseCoordinates,
            long iCurrentTimeStamp, bool bAllFrames)
        {
            //--------------------------------------------------------------------------------------
            // Change the UserAction if we are on a Drawing, Track, etc.
            // When we later pass in the MouseMove function, we will have the right UserAction set
            // and we will be able to do the right thing.
            //
            // We give priority to Keyframes Drawings because they can be moved...
            // If a Drawing is under a Trajectory or Chrono, we have to be able to move it around...
            //
            // Maybe we could reuse the IsOndrawing, etc. functions from MetaData...
            //--------------------------------------------------------------------------------------

            var bHit = true;
            _mUserAction = UserAction.None;

            metadata.UnselectAll();

            if (!IsOnDrawing(metadata, iActiveKeyFrameIndex, mouseCoordinates, iCurrentTimeStamp, bAllFrames))
            {
                if (!IsOnTrack(metadata, mouseCoordinates, iCurrentTimeStamp))
                {
                    if (!IsOnExtraDrawing(metadata, mouseCoordinates, iCurrentTimeStamp))
                    {
                        // Moving the whole image (Direct Zoom)
                        _mSelectedObjectType = SelectedObjectType.None;
                        bHit = false;
                    }
                }
            }

            // Store position (descaled: in original image coords).
            _mLastPoint.X = mouseCoordinates.X;
            _mLastPoint.Y = mouseCoordinates.Y;

            return bHit;
        }

        public bool OnMouseMove(Metadata metadata, Point mouseLocation, Point directZoomTopLeft, Keys modifierKeys)
        {
            // Note: We work with descaled coordinates.
            // Note: We only get here if left mouse button is down.

            var bIsMovingAnObject = true;
            var deltaX = 0;
            var deltaY = 0;

            if (_mDirectZoomTopLeft.X == -1)
            {
                // Initialize the zoom offset.
                _mDirectZoomTopLeft = new Point(directZoomTopLeft.X, directZoomTopLeft.Y);
            }

            // Find difference between previous and current position
            // X and Y are independant so we can slide on the edges in case of DrawingMove.
            if (mouseLocation.X >= 0 && mouseLocation.X <= _mImgSize.Width)
            {
                deltaX = (mouseLocation.X - _mLastPoint.X) - (directZoomTopLeft.X - _mDirectZoomTopLeft.X);
                _mLastPoint.X = mouseLocation.X;
            }
            if (mouseLocation.Y >= 0 && mouseLocation.Y <= _mImgSize.Height)
            {
                deltaY = (mouseLocation.Y - _mLastPoint.Y) - (directZoomTopLeft.Y - _mDirectZoomTopLeft.Y);
                _mLastPoint.Y = mouseLocation.Y;
            }

            MouseDelta = new Point(deltaX, deltaY);
            _mDirectZoomTopLeft = new Point(directZoomTopLeft.X, directZoomTopLeft.Y);

            if (deltaX != 0 || deltaY != 0)
            {
                switch (_mUserAction)
                {
                    case UserAction.Move:
                        {
                            switch (_mSelectedObjectType)
                            {
                                case SelectedObjectType.ExtraDrawing:
                                    if (metadata.SelectedExtraDrawing >= 0)
                                    {
                                        metadata.ExtraDrawings[metadata.SelectedExtraDrawing].MoveDrawing(deltaX, deltaY,
                                            modifierKeys);
                                    }
                                    break;

                                case SelectedObjectType.Drawing:
                                    if (metadata.SelectedDrawingFrame >= 0 && metadata.SelectedDrawing >= 0)
                                    {
                                        metadata.Keyframes[metadata.SelectedDrawingFrame].Drawings[metadata.SelectedDrawing]
                                            .MoveDrawing(deltaX, deltaY, modifierKeys);
                                    }
                                    break;

                                default:
                                    bIsMovingAnObject = false;
                                    break;
                            }
                        }
                        break;

                    case UserAction.Resize:
                        {
                            switch (_mSelectedObjectType)
                            {
                                case SelectedObjectType.ExtraDrawing:
                                    if (metadata.SelectedExtraDrawing >= 0)
                                    {
                                        metadata.ExtraDrawings[metadata.SelectedExtraDrawing].MoveHandle(mouseLocation,
                                            _mIResizingHandle);
                                    }
                                    break;

                                case SelectedObjectType.Drawing:
                                    if (metadata.SelectedDrawingFrame >= 0 && metadata.SelectedDrawing >= 0)
                                    {
                                        metadata.Keyframes[metadata.SelectedDrawingFrame].Drawings[metadata.SelectedDrawing]
                                            .MoveHandle(mouseLocation, _mIResizingHandle);
                                    }
                                    break;

                                default:
                                    bIsMovingAnObject = false;
                                    break;
                            }
                        }
                        break;

                    default:
                        bIsMovingAnObject = false;
                        break;
                }
            }
            else
            {
                bIsMovingAnObject = false;
            }

            return bIsMovingAnObject;
        }

        public void SetImageSize(Size size)
        {
            _mImgSize = new Size(size.Width, size.Height);
        }

        public void SetZoomLocation(Point point)
        {
            _mDirectZoomTopLeft = new Point(point.X, point.Y);
        }

        public Cursor GetCursor(int type)
        {
            // 0: Open hand, 1: Closed hand, -1: same as last time.

            var cur = _mCurHandOpen;
            switch (type)
            {
                case -1:
                    cur = (_mILastCursorType == 0) ? _mCurHandOpen : _mCurHandClose;
                    break;

                case 1:
                    cur = _mCurHandClose;
                    break;
            }

            return cur;
        }

        #endregion Public Interface

        #region Helpers

        private bool IsOnDrawing(Metadata metadata, int iActiveKeyFrameIndex, Point mouseCoordinates,
            long iCurrentTimeStamp, bool bAllFrames)
        {
            var bIsOnDrawing = false;

            if (bAllFrames && metadata.Keyframes.Count > 0)
            {
                var zOrder = metadata.GetKeyframesZOrder(iCurrentTimeStamp);

                for (var i = 0; i < zOrder.Length; i++)
                {
                    bIsOnDrawing = DrawingHitTest(metadata, zOrder[i], mouseCoordinates, iCurrentTimeStamp);
                    if (bIsOnDrawing)
                    {
                        break;
                    }
                }
            }
            else if (iActiveKeyFrameIndex >= 0)
            {
                bIsOnDrawing = DrawingHitTest(metadata, iActiveKeyFrameIndex, mouseCoordinates,
                    metadata[iActiveKeyFrameIndex].Position);
            }

            return bIsOnDrawing;
        }

        private bool DrawingHitTest(Metadata metadata, int iKeyFrameIndex, Point mouseCoordinates,
            long iCurrentTimeStamp)
        {
            var bDrawingHit = false;
            var kf = metadata.Keyframes[iKeyFrameIndex];
            var hitRes = -1;
            var iCurrentDrawing = 0;

            while (hitRes < 0 && iCurrentDrawing < kf.Drawings.Count)
            {
                hitRes = kf.Drawings[iCurrentDrawing].HitTest(mouseCoordinates, iCurrentTimeStamp);
                if (hitRes >= 0)
                {
                    bDrawingHit = true;
                    _mSelectedObjectType = SelectedObjectType.Drawing;
                    metadata.SelectedDrawing = iCurrentDrawing;
                    metadata.SelectedDrawingFrame = iKeyFrameIndex;

                    // Handler hit ?
                    if (hitRes > 0)
                    {
                        _mUserAction = UserAction.Resize;
                        _mIResizingHandle = hitRes;
                    }
                    else
                    {
                        _mUserAction = UserAction.Move;
                    }
                }
                else
                {
                    iCurrentDrawing++;
                }
            }

            return bDrawingHit;
        }

        private bool IsOnExtraDrawing(Metadata metadata, Point mouseCoordinates, long iCurrentTimeStamp)
        {
            // Test if we hit an unattached drawing.

            var bIsOnDrawing = false;
            var hitRes = -1;
            var iCurrentDrawing = 0;

            while (hitRes < 0 && iCurrentDrawing < metadata.ExtraDrawings.Count)
            {
                hitRes = metadata.ExtraDrawings[iCurrentDrawing].HitTest(mouseCoordinates, iCurrentTimeStamp);
                if (hitRes >= 0)
                {
                    bIsOnDrawing = true;
                    _mSelectedObjectType = SelectedObjectType.ExtraDrawing;
                    metadata.SelectedExtraDrawing = iCurrentDrawing;

                    // Handler hit ?
                    if (hitRes > 0)
                    {
                        _mUserAction = UserAction.Resize;
                        _mIResizingHandle = hitRes;
                    }
                    else
                    {
                        _mUserAction = UserAction.Move;
                    }
                }
                else
                {
                    iCurrentDrawing++;
                }
            }

            return bIsOnDrawing;
        }

        private bool IsOnTrack(Metadata metadata, Point mouseCoordinates, long iCurrentTimeStamp)
        {
            // Track have their own special hit test because we need to differenciate the interactive case from the edit case.
            var bTrackHit = false;

            for (var i = 0; i < metadata.ExtraDrawings.Count; i++)
            {
                var trk = metadata.ExtraDrawings[i] as Track;
                if (trk != null)
                {
                    // Result:
                    // -1 = miss, 0 = on traj, 1 = on Cursor, 2 = on main label, 3+ = on keyframe label.

                    var handle = trk.HitTest(mouseCoordinates, iCurrentTimeStamp);

                    if (handle >= 0)
                    {
                        bTrackHit = true;
                        _mSelectedObjectType = SelectedObjectType.ExtraDrawing;
                        metadata.SelectedExtraDrawing = i;

                        if (handle > 1)
                        {
                            // Touched target or handler.
                            // The handler would have been saved inside the track object.
                            _mUserAction = UserAction.Move;
                        }
                        else if (trk.Status == TrackStatus.Interactive)
                        {
                            _mUserAction = UserAction.Resize;
                            _mIResizingHandle = handle;
                        }
                        else
                        {
                            // edit mode + 0 or 1.
                            _mUserAction = UserAction.Move;
                        }

                        break;
                    }
                }
            }

            return bTrackHit;
        }

        private void SetupHandCursors()
        {
            // Hand cursor.
            Bitmap bmpOpen = Properties.Drawings.handopen24c;
            _mCurHandOpen = new Cursor(bmpOpen.GetHicon());

            Bitmap bmpClose = Properties.Drawings.handclose24b;
            _mCurHandClose = new Cursor(bmpClose.GetHicon());

            _mILastCursorType = 0;
        }

        #endregion Helpers
    }
}