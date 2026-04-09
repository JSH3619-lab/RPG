' ═══════════════════════════════════════════════════════════════
' Module: modPartGenerator (v5)
'
' 입고&Comp 시트:
'   좌측 C열 (공통): C3~C14 = Sourcing~CompType2 → DDD + Comp 양쪽에 사용
'   우측 F열 (Comp 전용): F3=PkgType, F4=Tester (2개만)
'   자동표시: F7=CompSourcing, F8=CompType, F9=DieBrand,
'             F10=Vendor, F11=Purchaser, F12=CompType2
'
' Comp DLK 조합 (Rev 28 p.4~5):
'   Sourcing(2) + Type + Density + Bit + Bank + Interface + Rev
'   - CompType + PkgType + DieBrand + Tester + Vendor + [Purchaser] + [CompType2]
'   (PkgType/Tester만 Comp 전용, 나머지 전부 DDD에서 상속)
' ═══════════════════════════════════════════════════════════════
Option Explicit

Private Function Ext(ByVal s As String) As String
    If s = "" Then Ext = "": Exit Function
    If s = "(없음)" Or Left(s, 1) = "(" Then Ext = "0": Exit Function
    If InStr(s, " - ") > 0 Then Ext = Trim(Left(s, InStr(s, " - ") - 1)) Else Ext = Trim(s)
End Function

Private Sub WLog(sT As String, sS As String, sP As String, sD As String)
    Dim ws As Worksheet: Set ws = ThisWorkbook.Sheets("Log")
    Dim r As Long: r = ws.Cells(ws.Rows.Count, 1).End(xlUp).Row + 1
    If r < 2 Then r = 2
    ws.Cells(r, 1) = r - 1: ws.Cells(r, 2) = Format(Now, "yyyy-mm-dd hh:nn:ss")
    ws.Cells(r, 3) = sT: ws.Cells(r, 4) = sS: ws.Cells(r, 5) = sP: ws.Cells(r, 6) = sD
End Sub

Private Function IsDup(ws As Worksheet, sP As String, iC As Long, iS As Long) As Boolean
    Dim rng As Range
    Set rng = ws.Range(ws.Cells(iS, iC), ws.Cells(ws.Rows.Count, iC)).Find(sP, LookIn:=xlValues, LookAt:=xlWhole)
    IsDup = Not rng Is Nothing
End Function


' ═══════════════════════════════════════════
'  입고 & Comp 동시 생성
' ═══════════════════════════════════════════
Public Sub GenerateDDDComp()
    Dim ws As Worksheet: Set ws = ThisWorkbook.Sheets("입고&Comp")
    
    ' 공통 필드 (C3~C14) — DDD + Comp 양쪽 사용
    Dim sSrc As String, sType As String, sDen As String, sBit As String
    Dim sBnk As String, sItf As String, sRev As String
    Dim sCT As String, sDB As String, sVen As String, sPur As String, sCT2 As String
    
    sSrc = Ext(ws.Range("C3").Value): sType = Ext(ws.Range("C4").Value)
    sDen = Ext(ws.Range("C5").Value): sBit = Ext(ws.Range("C6").Value)
    sBnk = Ext(ws.Range("C7").Value): sItf = Ext(ws.Range("C8").Value)
    sRev = Ext(ws.Range("C9").Value): sCT = Ext(ws.Range("C10").Value)
    sDB = Ext(ws.Range("C11").Value): sVen = Ext(ws.Range("C12").Value)
    sPur = Ext(ws.Range("C13").Value): sCT2 = Ext(ws.Range("C14").Value)
    
    ' Comp 전용 (F3~F4, 2개만)
    Dim sPkg As String, sTst As String
    sPkg = Ext(ws.Range("F3").Value)
    sTst = Ext(ws.Range("F4").Value)
    
    ' 필수 체크
    If sSrc = "" Or sType = "" Or sDen = "" Or sBit = "" Or sRev = "" Or _
       sCT = "" Or sDB = "" Or sVen = "" Then
        MsgBox "공통 필수 항목 누락 (Sourcing ~ Vendor)", vbExclamation: Exit Sub
    End If
    If sPkg = "" Or sTst = "" Then
        MsgBox "Comp 전용 필수 항목 누락 (Package Type, Test 업체)", vbExclamation: Exit Sub
    End If
    
    ' Validation
    Dim sErr As String: sErr = ""
    If sType = "A" Then
        If sDen <> "4G" And sDen <> "8G" And sDen <> "AG" Then sErr = "DDR4: Density 4G/8G/AG만" & vbCrLf
    ElseIf sType = "R" Then
        If sDen <> "AH" And sDen <> "HE" And sDen <> "BH" Then sErr = "DDR5: Density AH/HE/BH만" & vbCrLf
    End If
    If sErr <> "" Then MsgBox sErr, vbExclamation, "Validation": Exit Sub
    
    ' ── 입고 DDD 조합 (Rev 28 p.2~3) ──
    ' Sourcing + "4" + Type + Density + Bit + Bank + Interface + Rev - CompType + DieBrand + Vendor + "EL" + [Purchaser] + [CompType2]
    Dim sDDD As String
    sDDD = sSrc & "4" & sType & sDen & sBit & sBnk & sItf & sRev
    sDDD = sDDD & "-" & sCT & sDB & sVen & "EL"
    If sPur <> "0" Then sDDD = sDDD & sPur
    If sCT2 <> "0" Then sDDD = sDDD & sCT2
    
    ' ── Comp DLK 조합 (Rev 28 p.4~5) ──
    ' Sourcing(2) + Type + Density + Bit + Bank + Interface + Rev - CompType + PkgType + DieBrand + Tester + Vendor + [Purchaser] + [CompType2]
    Dim sSrcC As String
    Select Case sSrc
        Case "K": sSrcC = "RC": Case "T": sSrcC = "TC"
        Case "C": sSrcC = "CC": Case "B": sSrcC = "BC"
        Case Else: sSrcC = "RC"
    End Select
    
    Dim sComp As String
    sComp = sSrcC & sType & sDen & sBit & sBnk & sItf & sRev
    sComp = sComp & "-" & sCT & sPkg & sDB & sTst & sVen
    If sPur <> "0" Then sComp = sComp & sPur
    If sCT2 <> "0" Then sComp = sComp & sCT2
    
    ' 미리보기
    ws.Range("C17").Value = sDDD
    ws.Range("C18").Value = sComp
    
    ' 중복
    If IsDup(ws, sDDD, 3, 26) Then
        If MsgBox("입고 중복: " & sDDD & vbCrLf & "추가?", vbQuestion + vbYesNo) = vbNo Then Exit Sub
    End If
    
    Dim r As Long: r = ws.Cells(ws.Rows.Count, 3).End(xlUp).Row + 1
    If r < 26 Then r = 26
    Dim sN As String: sN = Format(Now, "yyyy-mm-dd hh:nn:ss")
    
    ' 행1: 입고
    ws.Cells(r, 1) = r - 25: ws.Cells(r, 2) = "입고"
    ws.Cells(r, 3) = sDDD: ws.Cells(r, 4) = sDDD
    ws.Cells(r, 5) = "": ws.Cells(r, 6) = "": ws.Cells(r, 7) = sN
    ' 행2: Comp
    r = r + 1
    ws.Cells(r, 1) = r - 25: ws.Cells(r, 2) = "Comp"
    ws.Cells(r, 3) = sComp: ws.Cells(r, 4) = sComp
    ws.Cells(r, 5) = "": ws.Cells(r, 6) = "": ws.Cells(r, 7) = sN
    
    WLog "생성", "입고&Comp", sDDD, "입고 | " & sType & "/" & sDen & "/" & sCT
    WLog "생성", "입고&Comp", sComp, "Comp | Pkg=" & sPkg & "/Tst=" & sTst
    
    MsgBox "입고+Comp 동시 생성:" & vbCrLf & vbCrLf & _
        "입고: " & sDDD & vbCrLf & "Comp: " & sComp, vbInformation, "완료"
End Sub


' ═══════════════════════════════════════════
'  Module 생성
' ═══════════════════════════════════════════
Public Sub GenerateModule()
    Dim ws As Worksheet: Set ws = ThisWorkbook.Sheets("Module_입력")
    Dim sSrc As String, sDT As String, sDIMM As String, sMD As String
    Dim sBV As String, sCmp As String, sDD As String, sRnk As String
    Dim sGen As String, sCT As String
    sSrc = Ext(ws.Range("C3").Value): sDT = Ext(ws.Range("C4").Value)
    sDIMM = Ext(ws.Range("C5").Value): sMD = Ext(ws.Range("C6").Value)
    sBV = Ext(ws.Range("C7").Value): sCmp = Ext(ws.Range("C8").Value)
    sDD = Ext(ws.Range("C9").Value): sRnk = Ext(ws.Range("C10").Value)
    sGen = Ext(ws.Range("C11").Value): sCT = Ext(ws.Range("C12").Value)
    
    Dim sCTst As String, sSMT As String, sMTst As String, sSpd As String
    Dim sPCB As String, sVen As String, sPur As String
    Dim sSp1 As String, sSp2 As String, sGC As String, sPB As String
    sCTst = Ext(ws.Range("F3").Value): sSMT = Ext(ws.Range("F4").Value)
    sMTst = Ext(ws.Range("F5").Value): sSpd = Ext(ws.Range("F6").Value)
    sPCB = Ext(ws.Range("F7").Value): sVen = Ext(ws.Range("F8").Value)
    sPur = Ext(ws.Range("F9").Value)
    sSp1 = Ext(ws.Range("F10").Value): sSp2 = Ext(ws.Range("F11").Value)
    sGC = Ext(ws.Range("F12").Value): sPB = Ext(ws.Range("F13").Value)
    
    If sSrc = "" Or sDT = "" Or sDIMM = "" Or sMD = "" Or sBV = "" Or _
       sCmp = "" Or sDD = "" Or sRnk = "" Or sGen = "" Or sCT = "" Or _
       sCTst = "" Or sSMT = "" Or sMTst = "" Or sSpd = "" Or sPCB = "" Or sVen = "" Then
        MsgBox "필수 항목 누락", vbExclamation: Exit Sub
    End If
    If sDT = "4" And sSpd <> "TD" And sSpd <> "WE" Then MsgBox "DDR4: Speed TD/WE만", vbExclamation: Exit Sub
    If sDT = "R" And (sSpd = "TD" Or sSpd = "WE") Then MsgBox "DDR5에 DDR4 Speed 불가", vbExclamation: Exit Sub
    
    Dim sBase As String
    sBase = sSrc & sDT & sDIMM & sMD & sBV & sCmp & sDD & sRnk & sGen
    sBase = sBase & "-" & sCT & sCTst & sSMT & sMTst & sSpd & sPCB & sVen
    If sPur <> "0" Then sBase = sBase & sPur
    If sSp1 <> "0" Then sBase = sBase & sSp1
    If sSp2 <> "0" Then sBase = sBase & sSp2
    
    If sGC = "" Then sGC = "TN"
    If sPB = "" Then sPB = "A00"
    Dim sBin As String: sBin = sBase & "-" & sGC & sMD & sPB
    
    Dim dM As Double, dD As Double, dB As Double, lI As Long, sIC As String
    Select Case sMD
        Case "1G": dM = 1: Case "2G": dM = 2: Case "4G": dM = 4
        Case "8G": dM = 8: Case "AG": dM = 16: Case "BG": dM = 32: Case "CG": dM = 64
    End Select
    Select Case sDD
        Case "4": dD = 4: Case "8": dD = 8: Case "A": dD = 16: Case "H": dD = 24: Case "B": dD = 32
    End Select
    Select Case sCmp
        Case "4": dB = 0.5: Case "8": dB = 1: Case "6": dB = 2
    End Select
    If dD * dB > 0 Then
        lI = CLng((dM * 8) / (dD * dB)): sIC = CStr(lI): If sRnk = "2" Then sIC = sIC & " (2R)"
    Else: sIC = "ERR"
    End If
    
    ws.Range("C14") = sIC: ws.Range("C17") = sBase: ws.Range("C18") = sBin
    
    If IsDup(ws, sBase, 2, 24) Then
        If MsgBox("중복: " & sBase & vbCrLf & "추가?", vbQuestion + vbYesNo) = vbNo Then Exit Sub
    End If
    
    Dim r As Long: r = ws.Cells(ws.Rows.Count, 2).End(xlUp).Row + 1
    If r < 24 Then r = 24
    Dim sN As String: sN = Format(Now, "yyyy-mm-dd hh:nn:ss")
    ws.Cells(r, 1) = r - 23: ws.Cells(r, 2) = sBase: ws.Cells(r, 3) = sBase
    ws.Cells(r, 4) = "": ws.Cells(r, 5) = "": ws.Cells(r, 6) = sIC: ws.Cells(r, 7) = "기본": ws.Cells(r, 8) = sN
    r = r + 1
    ws.Cells(r, 1) = r - 23: ws.Cells(r, 2) = sBin: ws.Cells(r, 3) = sBin
    ws.Cells(r, 4) = "": ws.Cells(r, 5) = "": ws.Cells(r, 6) = sIC: ws.Cells(r, 7) = "BIN체계": ws.Cells(r, 8) = sN
    
    WLog "생성", "Module", sBase, "기본+BIN | IC=" & sIC
    MsgBox "Module 2행:" & vbCrLf & "기본: " & sBase & vbCrLf & "BIN: " & sBin & vbCrLf & "IC: " & sIC, vbInformation
End Sub


' ═══════════════════════════════════════════
Public Sub ClearDDDComp()
    Dim ws As Worksheet: Set ws = ThisWorkbook.Sheets("입고&Comp")
    Dim i As Long
    For i = 3 To 14: ws.Cells(i, 3) = "": Next i
    ws.Range("F3") = "": ws.Range("F4") = ""
    Dim j As Long
    For j = 7 To 12: ws.Cells(j, 6) = "(자동)": Next j
    ws.Range("C17") = "": ws.Range("C18") = ""
    Dim rng As Range
    For Each rng In ws.Range("C3:C14,F3:F4")
        rng.Interior.Color = RGB(255, 255, 204): rng.Font.Color = RGB(0, 0, 0)
    Next rng
End Sub

Public Sub ClearModule()
    Dim ws As Worksheet: Set ws = ThisWorkbook.Sheets("Module_입력")
    Dim i As Long
    For i = 3 To 12: ws.Cells(i, 3) = "": Next i
    For i = 3 To 13: ws.Cells(i, 6) = "": Next i
    ws.Range("C14") = "(자동계산)": ws.Range("C17") = "": ws.Range("C18") = ""
    Dim rng As Range
    For Each rng In ws.Range("C3:C12,F3:F13")
        rng.Interior.Color = RGB(255, 255, 204): rng.Font.Color = RGB(0, 0, 0)
    Next rng
End Sub

Public Sub CreateAllButtons()
    Dim ws1 As Worksheet: Set ws1 = ThisWorkbook.Sheets("입고&Comp")
    Call RemBtn(ws1)
    Dim b As Object
    Set b = ws1.Buttons.Add(ws1.Range("B15").Left, ws1.Range("B15").Top, 80, 24)
    b.OnAction = "GenerateDDDComp": b.Characters.Text = "생성": b.Characters.Font.Size = 10: b.Characters.Font.Bold = True
    Set b = ws1.Buttons.Add(ws1.Range("B15").Left + 90, ws1.Range("B15").Top, 80, 24)
    b.OnAction = "ClearDDDComp": b.Characters.Text = "초기화": b.Characters.Font.Size = 10
    
    Dim ws2 As Worksheet: Set ws2 = ThisWorkbook.Sheets("Module_입력")
    Call RemBtn(ws2)
    Set b = ws2.Buttons.Add(ws2.Range("B15").Left, ws2.Range("B15").Top, 80, 24)
    b.OnAction = "GenerateModule": b.Characters.Text = "생성": b.Characters.Font.Size = 10: b.Characters.Font.Bold = True
    Set b = ws2.Buttons.Add(ws2.Range("B15").Left + 90, ws2.Range("B15").Top, 80, 24)
    b.OnAction = "ClearModule": b.Characters.Text = "초기화": b.Characters.Font.Size = 10
    MsgBox "버튼 생성 완료", vbInformation
End Sub

Private Sub RemBtn(ws As Worksheet)
    Dim s As Shape: For Each s In ws.Shapes: If s.Type = msoFormControl Then s.Delete: Next s
End Sub
