Imports System.Runtime.InteropServices
Imports System.ComponentModel
Imports System.Drawing
Imports System.Drawing.Imaging
Imports System.Windows.Forms
Imports System.Threading

Public Class ShaderScreen
    Inherits Forms.UserControl

    Private _alpha As Integer
    Private _zone As Point

    Private knownColor As Hashtable = New Hashtable()

    Public Event Clicked(sender As Object, e As EventArgs)

    Public Sub New()

        ' Cet appel est requis par le concepteur.
        InitializeComponent()

        Me.Visible = False

        _alpha = 50
        _zone = New Point(50, 50)

    End Sub

#Region " Property "
    <Description("Alpha property for the background. Value is the percentage of darkness.")>
    Public Property Alpha() As Integer
        Get
            Return _alpha
        End Get
        Set(value As Integer)
            _alpha = Math.Max(Math.Min(value, 100), 0)
        End Set
    End Property
    <Description("Real location of the box from the screen.")>
    Public Property Zone() As Point
        Get
            Return _zone
        End Get
        Set(value As Point)
            _zone = value
        End Set
    End Property
#End Region

    Public Sub picUpdate()

        Dim bmp As Bitmap = New Bitmap(Me.Size.Width, Me.Size.Height, System.Drawing.Imaging.PixelFormat.Format24bppRgb)
        Dim graph As Graphics = Graphics.FromImage(bmp)
        graph.CopyFromScreen(Me._zone, New Point(0, 0), Me.Size, CopyPixelOperation.MergeCopy)
        graph.Dispose()

        ' Display background while processing
        Me.shader.Image = New Bitmap(bmp)

        MyBase.Invoke(Sub() Visible = True)

        Dim time = System.DateTime.Now
        Dim new_bmp As Bitmap = Nothing
        new_bmp = applyShader_2(bmp)
        Console.WriteLine("Time spend " & (System.DateTime.Now - time).Milliseconds & "ms")

        If Not new_bmp Is Nothing Then
            Me.shader.Image = new_bmp
            bmp.Dispose()
        End If

    End Sub
    Private Sub applyShader_1(bmp As Bitmap)

        ' This naive method work fine but it takes 800ms in average
        ' By using hashtable, the time will be reduced to 500ms in average

        For i As Integer = 0 To bmp.Width - 1
            For j As Integer = 0 To bmp.Height - 1

                Dim pix As Color = bmp.GetPixel(i, j)

                If (Not knownColor.ContainsKey(pix)) Then

                    Dim r As Integer = Math.Max(pix.R - (_alpha * 255 / 100), 0)
                    Dim g As Integer = Math.Max(pix.G - (_alpha * 255 / 100), 0)
                    Dim b As Integer = Math.Max(pix.B - (_alpha * 255 / 100), 0)

                    knownColor.Add(pix, Color.FromArgb(r, g, b))

                End If
                bmp.SetPixel(i, j, knownColor.Item(pix))

            Next
        Next

    End Sub
    <Security.SecurityCritical()>
    Private Function applyShader_2(bmpSource As Bitmap) As Bitmap

        ' This method have the best time, it takes 180ms up to 5ms

        Dim imageWidth As Integer = bmpSource.Width
        Dim imageHeight As Integer = bmpSource.Height

        Dim bmpDest As Bitmap = Nothing
        Dim bmpDataSource As BitmapData = Nothing
        Dim bmpDataDest As BitmapData = Nothing

        Try

            ' Create image's copy
            bmpDest = New Bitmap(
                imageWidth,
                imageHeight,
                bmpSource.PixelFormat
            )

            ' Lock bitmap in memory
            bmpDataDest = bmpDest.LockBits(
                New Rectangle(0, 0, imageWidth, imageHeight),
                Imaging.ImageLockMode.ReadWrite,
                bmpDest.PixelFormat
            )
            bmpDataSource = bmpSource.LockBits(
                New Rectangle(0, 0, imageWidth, imageHeight),
                Imaging.ImageLockMode.ReadOnly,
                bmpSource.PixelFormat
            )

            Dim pixelSize As Integer = getPixelInfoSize(bmpDataSource.PixelFormat)
            Dim buffer(imageWidth * imageHeight * pixelSize) As Byte
            Dim destBuffer(imageWidth * imageHeight * pixelSize) As Byte

            ' Read all data to buffer
            Dim addrStart As Integer = bmpDataSource.Scan0.ToInt32()

            For i As Integer = 0 To imageHeight - 1

                ' Get address of next row
                Dim source As IntPtr = New IntPtr(addrStart + System.Convert.ToInt32(i * bmpDataSource.Stride()))
                Dim startIndex As Integer = CInt(i * imageWidth * pixelSize)
                Dim length As Integer = CInt(imageWidth * pixelSize)

                ' Perform copy from managed buffer
                ' to unmanaged memory
                Marshal.Copy(
                        source,
                        buffer,
                        startIndex,
                        length
                    )

            Next i

            Dim opti(256) As Byte
            For i As Integer = 0 To 255
                opti(i) = i - ((i * _alpha) / 100)
            Next

            ' Do some stuff on buffer to apply alpha shader
            For i As Integer = 0 To buffer.Length - 1

                ' 180ms
                'Dim shader As Byte = CByte((buffer(i) * _alpha) / 100)
                'If (buffer(i) < shader) Then
                '    destBuffer(i) = 0
                'Else
                '    destBuffer(i) = CByte(buffer(i) - shader)
                'End If

                ' 160ms
                'destBuffer(i) = CByte(buffer(i) - ((buffer(i) * _alpha) / 100))

                ' 5ms
                destBuffer(i) = opti(buffer(i))

            Next

            ' Get unmanaged data start address
            addrStart = bmpDataDest.Scan0.ToInt32()

            For i As Integer = 0 To imageHeight - 1

                ' Get address of next row
                Dim dest As IntPtr = New IntPtr(addrStart + System.Convert.ToInt32(i * bmpDataDest.Stride()))
                Dim startIndex As Integer = CInt(i * imageWidth * pixelSize)
                Dim length As Integer = CInt(imageWidth * pixelSize)

                ' Perform copy from managed buffer
                ' to unmanaged memory
                Marshal.Copy(
                    destBuffer,
                    startIndex,
                    dest,
                    length
                )

            Next i

            Return bmpDest

        Catch ex As Exception
            Console.WriteLine(ex.Message)
        Finally
            If Not bmpDest Is Nothing Then bmpDest.UnlockBits(bmpDataDest)
            If Not bmpSource Is Nothing Then bmpSource.UnlockBits(bmpDataSource)
        End Try

        Return Nothing

    End Function
    Private Function getPixelInfoSize(ByVal format As PixelFormat)

        Select Case format
            Case PixelFormat.Format24bppRgb
                Return 3
            Case Else
                Throw New Exception("Format unsupported now")
        End Select

    End Function

    Private Sub shader_Click(sender As Object, e As EventArgs) Handles shader.Click
        RaiseEvent Clicked(sender, e)
    End Sub

End Class
