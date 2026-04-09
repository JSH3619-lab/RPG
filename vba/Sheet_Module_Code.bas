' ═══════════════════════════════════════════════════════════════
' Module_입력 시트 코드 (더블클릭해서 붙여넣기)
' ═══════════════════════════════════════════════════════════════
Option Explicit

Private Sub Worksheet_Change(ByVal Target As Range)
    If Target.Count > 1 Then Exit Sub
    If Target.Column <> 3 And Target.Column <> 6 Then Exit Sub
    
    Application.EnableEvents = False
    On Error GoTo Cleanup
    
    Dim sVal As String: sVal = Trim(CStr(Target.Value))
    Dim sCode As String
    If InStr(sVal, " - ") > 0 Then sCode = Left(sVal, InStr(sVal, " - ") - 1) Else sCode = sVal
    
    ' C3: Sourcing → RM/CM(non-TP): Purchaser(F9) 비활성
    If Target.Address = "$C$3" Then
        If sCode = "RM" Or sCode = "CM" Then
            Call LockCell(Me.Range("F9"), "(없음)")
        Else
            Call UnlockCell(Me.Range("F9"))
        End If
    End If
    
    ' C4: DRAM Type → Bank(C7) 강제 + Speed(F6) 필터
    If Target.Address = "$C$4" Then
        If sCode = "4" Then  ' DDR4
            Call LockCell(Me.Range("C7"), "4 - 16Bank/1.2V")
            Me.Range("F6").Value = ""
            Call SwapDV(Me, "$F$6", "MOD_Speed_DDR4")
        ElseIf sCode = "R" Then  ' DDR5
            Me.Range("C7").Value = "5 - 32Bank/1.1V"
            Me.Range("C7").Interior.Color = RGB(255, 255, 204)
            Me.Range("C7").Font.Color = RGB(0, 0, 0)
            Me.Range("F6").Value = ""
            Call SwapDV(Me, "$F$6", "MOD_Speed_DDR5")
        Else
            Call UnlockCell(Me.Range("C7"))
            Me.Range("F6").Value = ""
        End If
    End If

Cleanup:
    Application.EnableEvents = True
End Sub

Private Sub LockCell(rng As Range, sVal As String)
    rng.Value = sVal
    rng.Interior.Color = RGB(217, 217, 217)
    rng.Font.Color = RGB(128, 128, 128)
End Sub

Private Sub UnlockCell(rng As Range)
    rng.Interior.Color = RGB(255, 255, 204)
    rng.Font.Color = RGB(0, 0, 0)
End Sub

Private Sub SwapDV(ws As Worksheet, sAddr As String, sNR As String)
    Dim rng As Range: Set rng = ws.Range(sAddr)
    rng.Validation.Delete
    rng.Validation.Add Type:=xlValidateList, Formula1:="=" & sNR
End Sub
