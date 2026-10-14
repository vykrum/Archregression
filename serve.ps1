# Launch Archregression F# Bolero WebAssembly Studio
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host " 🏛️ Archregression: F# Bolero WebAssembly (.NET 10) Studio" -ForegroundColor Cyan
Write-Host "==========================================================" -ForegroundColor Cyan
Write-Host "Launching Studio at http://localhost:5050 ..." -ForegroundColor Gray

dotnet run --project src/Archregression --urls "http://localhost:5050"
