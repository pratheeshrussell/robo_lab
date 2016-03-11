Imports Emgu.CV
Imports Emgu.CV.CvEnum
Imports Emgu.CV.Structure
Imports Emgu.CV.UI
Imports System.Drawing

Public Class Class1

   

    Function detect_circles(ByVal imgOriginal As Image(Of Bgr, Byte), ByVal mycol As Bgr) As Image(Of Bgr, Byte)
        Dim imgSmoothed As Image(Of Bgr, Byte)
        Dim imgGrayColorFiltered As Image(Of Gray, Byte)
        Dim imgCanny As Image(Of Gray, Byte)
       
        imgSmoothed = imgOriginal.PyrDown().PyrUp()
        imgSmoothed._SmoothGaussian(3)
        imgGrayColorFiltered = imgSmoothed.Convert(Of Gray, Byte)()
        Dim grayCannyThreshold As Gray = New Gray(160)
        Dim grayCircleAccumThreshold As Gray = New Gray(100)
        imgCanny = imgGrayColorFiltered.Canny(160, 80)
        Dim dblAccumRes As Double = 2.0
        Dim dblMinDistBetweenCircles As Double = imgGrayColorFiltered.Height / 4
        Dim intMinRadius As Integer = 10
        Dim intMaxRadius As Integer = 400
        'find circles
        Dim circles As CircleF() = imgGrayColorFiltered.HoughCircles(grayCannyThreshold, grayCircleAccumThreshold, dblAccumRes, dblMinDistBetweenCircles, intMinRadius, intMaxRadius)(0)

        For Each circle As CircleF In circles
            imgOriginal.Draw(circle, mycol, 10)
        Next
        Return imgOriginal
    End Function

    Function detect_polygons(ByVal imgOriginal As Image(Of Bgr, Byte), ByVal mycol As Bgr) As Image(Of Bgr, Byte)
        Dim lstPolygons As List(Of Contour(Of Point)) = get_list(imgOriginal, "poly")
        For Each contPoly As Contour(Of Point) In lstPolygons
            imgOriginal.Draw(contPoly, mycol, 10)                                  'then also draw polygons on original image
        Next
        Return imgOriginal
    End Function
    Function detect_triangles(ByVal imgOriginal As Image(Of Bgr, Byte), ByVal mycol As Bgr) As Image(Of Bgr, Byte)
        Dim lstTriangles As List(Of Triangle2DF) = get_list(imgOriginal, "tri")
        For Each triangle As Triangle2DF In lstTriangles
            imgOriginal.Draw(triangle, mycol, 10)
        Next
        Return imgOriginal
    End Function

    Function detect_rectangles(ByVal imgOriginal As Image(Of Bgr, Byte), ByVal mycol As Bgr) As Image(Of Bgr, Byte)
        Dim lstRectangles As List(Of MCvBox2D) = get_list(imgOriginal, "rect")
        For Each rect As MCvBox2D In lstRectangles 'add the rectangles to the image
            imgOriginal.Draw(rect, mycol, 10) 'then also draw rectangles on original image
        Next
        Return imgOriginal
    End Function

    Function get_list(ByVal imgOriginal As Image(Of Bgr, Byte), ByVal type As String)
        Dim imgSmoothed As Image(Of Bgr, Byte)
        Dim imgGrayColorFiltered As Image(Of Gray, Byte)
        Dim imgCanny As Image(Of Gray, Byte)

        imgSmoothed = imgOriginal.PyrDown().PyrUp()                                     'Gaussian pyramid decomposition
        imgSmoothed._SmoothGaussian(3)
        imgGrayColorFiltered = imgSmoothed.Convert(Of Gray, Byte)()
        Dim grayCannyThreshold As Gray = New Gray(160)                                  'first Canny threshold, used for both circle detection, and line / triangle / rectangle detection
        Dim grayCircleAccumThreshold As Gray = New Gray(100)                        'second Canny threshold for circle detection, higher number = more selective
        Dim grayThreshLinking As Gray = New Gray(80)                                        'second Canny threshold for line / triangle / rectangle detection
        imgCanny = imgGrayColorFiltered.Canny(160, 80)

        Dim contours As Contour(Of Point) = imgCanny.FindContours()
        Dim lstTriangles As List(Of Triangle2DF) = New List(Of Triangle2DF)()                               'declare list of triangles
        Dim lstRectangles As List(Of MCvBox2D) = New List(Of MCvBox2D)()                                        'declare list of "rectangles"
        Dim lstPolygons As List(Of Contour(Of Point)) = New List(Of Contour(Of Point))

        While (Not contours Is Nothing)
            Dim contour As Contour(Of Point) = contours.ApproxPoly(10.0)                                            'approximates one or more curves and returns the approximation results
            If (contour.Area > 250.0) Then
                If (contour.Total = 3) Then                                                                                                          'if 3 points, it's a triangle
                    Dim ptPoints() As Point = contour.ToArray()                                                                     'get contour points
                    lstTriangles.Add(New Triangle2DF(ptPoints(0), ptPoints(1), ptPoints(2)))            'and add to triangle list
                ElseIf (contour.Total >= 4 And contour.Total <= 6) Then   'if 4, 5, or 6 points, could be a square or a polygon
                    Dim ptPoints() As Point = contour.ToArray()                                                                     'get contour points
                    Dim blnIsRectangle As Boolean = True                                                                                    'to start with, lets suppose it's a rectangle

                    If (contour.Total = 4) Then                                                                                                              'if 4 points, could be a rectangle . . .
                        Dim ls2dEdges As LineSegment2D() = PointCollection.PolyLine(ptPoints, True)         'get edges between points
                        For i As Integer = 0 To ls2dEdges.Length - 1                                                                        'step through edges
                            Dim dblAngle As Double = Math.Abs(ls2dEdges((i + 1) Mod ls2dEdges.Length).GetExteriorAngleDegree(ls2dEdges(i)))
                            If (dblAngle < 80.0 Or dblAngle > 100.0) Then                                                                    'if not about a 90 degree angle between edges
                                blnIsRectangle = False                                                                                                          'then it's not a rectangle
                                Exit For                                                                                                                                        'note that if execution never gets here, blnIsRectangle will stay True as initialized
                            End If
                        Next
                    Else                                                            'if more than 4 points,
                        blnIsRectangle = False                  'can't possibly be a rectangle
                    End If

                    If (blnIsRectangle) Then                                                             'if a rectangle
                        lstRectangles.Add(contour.GetMinAreaRect())                 'add to list of rectangles
                    Else                                                                                                    'otherwise
                        lstPolygons.Add(contour)                                                        'add to list of polygons
                    End If
                End If
            End If
            contours = contours.HNext                           'go to next contour in countours sequence
        End While

        If type = "rect" Then
            Return lstRectangles
        End If
        If type = "tri" Then
            Return lstTriangles
        End If
        If type = "poly" Then
            Return lstPolygons
        End If
    End Function



    Function about()
        MsgBox("This Library was created by Pratheesh", 0, "About:")
    End Function
End Class
