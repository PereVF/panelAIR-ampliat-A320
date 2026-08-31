$ErrorActionPreference = "Stop"

$baseDir = "G:\Mi unidad\carpeta compartida VAIO, ALEIN, LENOVO"
$copiaDir = Get-ChildItem -LiteralPath $baseDir -Directory | Where-Object { $_.Name -like "*PIA DE SEGURETAT" } | Select-Object -First 1
$targetDir = Join-Path $copiaDir.FullName "PERE\SFS-SimFlightServices\PRODUCTS\6-panelAIR"
$filePath = Join-Path $targetDir "compatibilitat_panelAIR.xlsx"
$lockFile = Join-Path $targetDir "~`$compatibilitat_panelAIR.xlsx"

$wasOpen = $false
if (Test-Path -LiteralPath $lockFile) {
    $wasOpen = $true
    try {
        $liveExcel = [System.Runtime.InteropServices.Marshal]::GetActiveObject("Excel.Application")
        foreach ($w in $liveExcel.Workbooks) {
            if ($w.Name -like "compatibilitat_panelAIR*") { $w.Close($true); break }
        }
    } catch {}
    Start-Sleep -Milliseconds 800
}

function RGB($r,$g,$b) { return $r + ($g*256) + ($b*65536) }
$colHeaderBg = RGB 30 41 59
$colHeaderFg = RGB 255 255 255
$colBorder   = RGB 203 213 225
$colV = RGB 198 246 213; $colVFg = RGB 22 101 52
$colP = RGB 254 240 199; $colPFg = RGB 146 64 14
$colX = RGB 254 202 202; $colXFg = RGB 153 27 27
$colNA = RGB 226 232 240; $colNAFg = RGB 71 85 105
$colCatBg = RGB 71 85 105; $colCatFg = RGB 255 255 255
$colLegendTitleBg = RGB 15 23 42; $colLegendTitleFg = RGB 255 255 255

$excel = New-Object -ComObject Excel.Application
$excel.Visible = $false
$excel.DisplayAlerts = $false
$wb = $excel.Workbooks.Open($filePath)
if ($wb.ReadOnly) { $wb.Close($false); $excel.Quit(); throw "Read-only." }
$ws = $wb.Worksheets.Item("Compatibilitat")

$lastCol = $ws.Cells.Item(2, $ws.Columns.Count).End(-4159).Column
$lastRow = $ws.Cells.Item($ws.Rows.Count, 1).End(-4162).Row

$acNameByCol = @{}
$acYearByCol = @{}
for ($c = 2; $c -le $lastCol; $c++) {
    $ma = $ws.Cells.Item(2,$c).MergeArea
    $acNameByCol[$c] = $ma.Cells.Item(1,1).Value2
    $acYearByCol[$c] = $ws.Cells.Item(3,$c).Value2
}
$topTitle = $ws.Cells.Item(1,2).MergeArea.Cells.Item(1,1).Value2
$sysLabel = $ws.Cells.Item(1,1).MergeArea.Cells.Item(1,1).Value2

$bodyRows = New-Object System.Collections.ArrayList
for ($r = 4; $r -le $lastRow; $r++) {
    $a = $ws.Cells.Item($r,1).Value2
    $isMerged1 = $ws.Cells.Item($r,1).MergeCells
    $rowData = [ordered]@{ Label = $a; Cells = @{}; Desc = $null; FullMerge = $false }
    if ($isMerged1) {
        $ma = $ws.Cells.Item($r,1).MergeArea
        if ($ma.Column -eq 1 -and $ma.Columns.Count -gt 1) { $rowData.FullMerge = $true }
    }
    if (-not $rowData.FullMerge) {
        for ($c = 2; $c -le $lastCol; $c++) {
            $v = $ws.Cells.Item($r,$c).Value2
            if ($v) { $rowData.Cells[$c] = $v }
        }
        if ($ws.Cells.Item($r,2).MergeCells) {
            $rowData.Desc = $ws.Cells.Item($r,2).MergeArea.Cells.Item(1,1).Value2
        }
    }
    [void]$bodyRows.Add($rowData)
}

# ---- CORREGIR el bug de "-like [TBM]" (classe de caracters, no text literal) ----
# Etiquetes QUE SI han de portar "TBM " al davant (les autentiques exclusives de la TBM):
$legitTbm = @(
    "TBM G3000 PFD", "TBM G3000 MFD", "TBM Touch Screen Control 1", "TBM Touch Screen Control 2",
    "TBM Source Switch (Batt/GPU)", "TBM Generator Switch (Off/Main/Stby)", "TBM Crashbar",
    "TBM Pulse Light", "TBM De-ice Inert Separator", "TBM"
)
# tambe qualsevol etiqueta que comenci per "TBM Throttle" (Throttle Quadrant, pot estar abreujada per Pere)
for ($i = 0; $i -lt $bodyRows.Count; $i++) {
    $lbl = $bodyRows[$i].Label
    if ($lbl -and $lbl.StartsWith("TBM ") -and ($legitTbm -notcontains $lbl) -and (-not $lbl.StartsWith("TBM Throttle"))) {
        $bodyRows[$i].Label = $lbl.Substring(4)
    }
}

# ---- Reconstruir (sense tocar res mes) ----
$ws.Application.ActiveWindow.FreezePanes = $false
$ws.Cells.Clear() | Out-Null

$topRng = $ws.Range($ws.Cells.Item(1,2), $ws.Cells.Item(1,$lastCol))
$topRng.Merge() | Out-Null
$topRng.Value2 = $topTitle

$sysRng = $ws.Range($ws.Cells.Item(1,1), $ws.Cells.Item(3,1))
$sysRng.Merge() | Out-Null
$sysRng.Value2 = $sysLabel

$c = 2
while ($c -le $lastCol) {
    $name = $acNameByCol[$c]
    $year = $acYearByCol[$c]
    if ($year -eq "2020" -and $acYearByCol.ContainsKey($c+1) -and $acYearByCol[$c+1] -eq "2024" -and $acNameByCol[$c+1] -eq $name) {
        $mr = $ws.Range($ws.Cells.Item(2,$c), $ws.Cells.Item(2,$c+1))
        $mr.Merge() | Out-Null
        $mr.Value2 = $name
        $ws.Cells.Item(3,$c).Value2 = "2020"
        $ws.Cells.Item(3,$c+1).Value2 = "2024"
        $c += 2
    } else {
        $mr = $ws.Range($ws.Cells.Item(2,$c), $ws.Cells.Item(3,$c))
        $mr.Merge() | Out-Null
        $mr.Value2 = $name
        $c += 1
    }
}

$headerRange = $ws.Range($ws.Cells.Item(1,1), $ws.Cells.Item(3,$lastCol))
$headerRange.Interior.Color = $colHeaderBg
$headerRange.Font.Color = $colHeaderFg
$headerRange.Font.Bold = $true
$headerRange.HorizontalAlignment = -4108
$headerRange.VerticalAlignment = -4108
$headerRange.WrapText = $true
$ws.Rows.Item(1).RowHeight = 22
$ws.Rows.Item(2).RowHeight = 26
$ws.Rows.Item(3).RowHeight = 16

$r = 4
foreach ($row in $bodyRows) {
    $label = $row.Label
    if ($row.FullMerge) {
        $ws.Cells.Item($r,1).Value2 = $label
        $rng = $ws.Range($ws.Cells.Item($r,1), $ws.Cells.Item($r,$lastCol))
        $rng.Merge() | Out-Null
        if ($label -eq "LEGEND" -or $label -eq "NOTES") {
            $rng.Interior.Color = $colLegendTitleBg
            $rng.Font.Color = $colLegendTitleFg
            $rng.Font.Bold = $true
        } elseif ($label) {
            $rng.Interior.Color = $colCatBg
            $rng.Font.Color = $colCatFg
            $rng.Font.Bold = $true
            $rng.HorizontalAlignment = -4131
            $ws.Cells.Item($r,1).IndentLevel = 1
        }
    }
    elseif ($label -in @("V","P","X","N/A","(blank)","TBM") -or ($label -and $label.StartsWith("*"))) {
        $c1 = $ws.Cells.Item($r,1)
        $c1.Value2 = $label
        $c1.Font.Bold = $true
        $c1.HorizontalAlignment = -4108
        if ($label -eq "V") { $c1.Interior.Color = $colV; $c1.Font.Color = $colVFg }
        elseif ($label -eq "P") { $c1.Interior.Color = $colP; $c1.Font.Color = $colPFg }
        elseif ($label -eq "X") { $c1.Interior.Color = $colX; $c1.Font.Color = $colXFg }
        elseif ($label -eq "N/A") { $c1.Interior.Color = $colNA; $c1.Font.Color = $colNAFg }
        $rng = $ws.Range($ws.Cells.Item($r,2), $ws.Cells.Item($r,$lastCol))
        $rng.Merge() | Out-Null
        $rng.Value2 = $row.Desc
        $rng.HorizontalAlignment = -4131
        if ($label.StartsWith("*")) { $rng.WrapText = $true; $ws.Rows.Item($r).RowHeight = 30 }
    }
    else {
        $ws.Cells.Item($r,1).Value2 = $label
        $ws.Cells.Item($r,1).IndentLevel = 1
        foreach ($kv in $row.Cells.GetEnumerator()) {
            $cell = $ws.Cells.Item($r, $kv.Key)
            $v = $kv.Value
            $displayVal = $v
            if ($v -eq "P*2") { $displayVal = "P *2" }
            if ($v -eq "P*3") { $displayVal = "P *3" }
            $cell.Value2 = $displayVal
            $cell.Font.Bold = $true
            $cell.HorizontalAlignment = -4108
            if ($v -like "V*") { $cell.Interior.Color = $colV; $cell.Font.Color = $colVFg }
            elseif ($v -like "P*") { $cell.Interior.Color = $colP; $cell.Font.Color = $colPFg }
            elseif ($v -like "X*") { $cell.Interior.Color = $colX; $cell.Font.Color = $colXFg }
            elseif ($v -eq "N/A") { $cell.Interior.Color = $colNA; $cell.Font.Color = $colNAFg }
        }
    }
    $r++
}
$lastRowNew = $r - 1

$usedRange = $ws.Range($ws.Cells.Item(1,1), $ws.Cells.Item($lastRowNew, $lastCol))
$usedRange.Borders.Color = $colBorder
$usedRange.Borders.Weight = 2

$ws.Columns.Item(1).ColumnWidth = 40
for ($cc = 2; $cc -le $lastCol; $cc++) { $ws.Columns.Item($cc).ColumnWidth = 7 }

$ws.Application.ActiveWindow.SplitColumn = 1
$ws.Application.ActiveWindow.SplitRow = 3
$ws.Application.ActiveWindow.FreezePanes = $true

$wb.Save()
$wb.Close($false)
$excel.Quit()
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($ws) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($wb) | Out-Null
[System.Runtime.Interopservices.Marshal]::ReleaseComObject($excel) | Out-Null
[GC]::Collect(); [GC]::WaitForPendingFinalizers()

if ($wasOpen) { Start-Process -FilePath $filePath }
Write-Output "FET"
