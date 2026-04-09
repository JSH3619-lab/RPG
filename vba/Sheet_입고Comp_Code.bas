' ═══════════════════════════════════════════════════════════════
' 입고&Comp 시트 코드 (v5)
' 복사: VBE > "입고&Comp" 시트 더블클릭 > 코드 창에 붙여넣기
' ═══════════════════════════════════════════════════════════════
Option Explicit

Private Sub Worksheet_Change(ByVal Target As Range)
    If Target.Count > 1 Then Exit Sub
    If Target.Column <> 3 Then Exit Sub  ' C열만 감시 (F열은 Comp 전용 2개뿐)
    
    Application.EnableEvents = False
    On Error GoTo Cleanup
    
    Dim sVal As String: sVal = Trim(CStr(Target.Value))
    Dim sCode As String
    If InStr(sVal, " - ") > 0 Then sCode = Left(sVal, InStr(sVal, " - ") - 1) Else sCode = sVal
    
    ' ── C3: Sourcing Type ──
    '   K,C → Purchaser(C13) 비활성 + Comp Sourcing 매핑
    If Target.Address = "$C$3" Then
        Dim sCSrc As String
        Select Case sCode
            Case "K": sCSrc = "RC": Case "T": sCSrc = "TC"
            Case "C": sCSrc = "CC": Case "B": sCSrc = "BC"
            Case Else: sCSrc = ""
        End Select
        Me.Range("F7").Value = sCSrc & " (자동)"
        
        If sCode = "K" Or sCode = "C" Then
            Call LockCell(Me.Range("C13"), "(없음)")
            Me.Range("F11").Value = "(없음) (자동)"
        Else
            Call UnlockCell(Me.Range("C13"))
            Me.Range("F11").Value = "(자동)"
        End If
    End If
    
    ' ── C4: DRAM Type ──
    '   DDR4→Bank=5,Interface=W / DDR5→Bank=6,Interface=V
    If Target.Address = "$C$4" Then
        If sCode = "A" Then
            Call LockCell(Me.Range("C7"), "5 - 16Bank")
            Call LockCell(Me.Range("C8"), "W - POD 1.2V")
            Me.Range("C5").Value = ""
            Call SwapDV(Me, "$C$5", "DDD_Density_DDR4")
        ElseIf sCode = "R" Then
            Call LockCell(Me.Range("C7"), "6 - 32Bank")
            Call LockCell(Me.Range("C8"), "V - POD 1.1V")
            Me.Range("C5").Value = ""
            Call SwapDV(Me, "$C$5", "DDD_Density_DDR5")
        Else
            Call UnlockCell(Me.Range("C7"))
            Call UnlockCell(Me.Range("C8"))
            Me.Range("C5").Value = ""
            Call SwapDV(Me, "$C$5", "DDD_Density_ALL")
        End If
    End If
    
    ' ── C10: CompType → F8 자동 ──
    If Target.Address = "$C$10" Then Me.Range("F8").Value = sVal & " (자동)"
    
    ' ── C11: DieBrand → F9 자동 ──
    If Target.Address = "$C$11" Then Me.Range("F9").Value = sVal & " (자동)"
    
    ' ── C12: Vendor → F10 자동 ──
    If Target.Address = "$C$12" Then Me.Range("F10").Value = sVal & " (자동)"
    
    ' ── C13: Purchaser → F11 자동 ──
    If Target.Address = "$C$13" Then Me.Range("F11").Value = sVal & " (자동)"
    
    ' ── C14: CompType2 → F12 자동 ──
    If Target.Address = "$C$14" Then Me.Range("F12").Value = sVal & " (자동)"

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
