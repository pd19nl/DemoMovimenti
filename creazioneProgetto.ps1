#NB se non viene eseguito:
#Scrivere in Power - shell:
#Set-ExecutionPolicy RemoteSigned -Scope Process


$ErrorActionPreference = "Stop"

#	---- --------------------------------------------------------------------------
#	---- Impostazioni
$NomeSoluzione ="DemoMovimenti"
$CartellaRoot = "$NomeSoluzione"
$Framework = "net9.0"
#	---- --------------------------------------------------------------------------
#	---- --------------------------------------------------------------------------


#	---- --------------------------------------------------------------------------
#	---- folder
Write-Host "1.0) Creazione folder root: $NomeSoluzione"
New-item -ItemType Directory -Force -Path $CartellaRoot
Set-Location $CartellaRoot
#	---- --------------------------------------------------------------------------
#	---- --------------------------------------------------------------------------


#	---- --------------------------------------------------------------------------
#	---- creazione soluzione
Write-Host "2.0) Creazione Soluzione root: $NomeSoluzione.sln"
dotnet new sln -n $NomeSoluzione
#	---- --------------------------------------------------------------------------
#	---- --------------------------------------------------------------------------


#	---- --------------------------------------------------------------------------
#	---- creazione Progetto 
Write-Host "3.0) Creazione Progetti"

	#	---- creazione Progetto Web Api: Ordini.Api
	Write-Host "3.1) Creazione Progetto Web API: $NomeSoluzione.sln"
	dotnet new webapi -n Ordini.Api -o Ordini.Api -f $Framework
	#	---- --------------------------------------------------------------------------
	

	#	---- creazione Progetto Worker : Ordini.Processor
	Write-Host "3.2) Creazione Progetto Worker: Ordini.Processor"
	dotnet new worker -n Ordini.Processor -o Ordini.Processor -f $Framework
	#	---- --------------------------------------------------------------------------
	

	#	---- creazione Progetto Worker: Pagamenti.Processor
	Write-Host "3.3) Creazione Progetto Worker: Pagamenti.Processor"
	dotnet new worker -n Pagamenti.Processor -o Pagamenti.Processor -f $Framework
	#	---- --------------------------------------------------------------------------


	#	---- creazione Progetto Worker: Inventario.Processor
	Write-Host "3.4) Creazione Progetto Worker: Inventario.Processor"
	dotnet new worker -n Inventario.Processor -o Inventario.Processor -f $Framework
	#	---- --------------------------------------------------------------------------


	#	---- creazione Progetto Worker: Notifiche.Processor
	Write-Host "3.5) Creazione Progetto Worker: Notifiche.Processor"
	dotnet new worker -n Notifiche.Processor -o Notifiche.Processor -f $Framework
	#	---- --------------------------------------------------------------------------


	#	---- creazione Progetto Libreria : Ordini.Contracts
	Write-Host "3.6) Creazione Progetto Libreria: Ordini.Contracts"
	dotnet new classlib -n Ordini.Contracts -o Ordini.Contracts -f $Framework
	#	---- --------------------------------------------------------------------------


	#	---- creazione Progetto Libreria : Ordini.ApplicationAPI.Models
	Write-Host "3.7) Creazione Progetto Libreria: Ordini.ApplicationAPI.Models"
	dotnet new classlib -n Ordini.ApplicationAPI.Models -o Ordini.ApplicationAPI.Models -f $Framework
	#	---- --------------------------------------------------------------------------

	
	#	---- creazione Progetto Libreria : Ordini.ApplicationAPI.Models
	Write-Host "3.7) Creazione Progetto Libreria: Ordini.ApplicationAPI.Models"
	dotnet new classlib -n Ordini.ApplicationAPI.Models -o Ordini.ApplicationAPI.Models -f $Framework
	#	---- --------------------------------------------------------------------------

		
	#	---- creazione Progetto Libreria : Ordini.ApplicationAPI.Models
	Write-Host "3.7) Creazione Progetto Libreria: Ordini.ApplicationAPI.Models"
	dotnet new classlib -n Ordini.ApplicationAPI.Models -o Ordini.ApplicationAPI.Models -f $Framework
	#	---- --------------------------------------------------------------------------
	
		
	#	---- creazione Progetto Libreria : Ordini.LySystem.Models.Repositories
	#Write-Host "3.8) Creazione Progetto Libreria: Ordini.LySystem.Models.Repositories"
	#dotnet new classlib -n Ordini.LySystem.Models.Repositories -o Ordini.LySystem.Models.Repositories -f $Framework
	#	---- --------------------------------------------------------------------------


#	---- --------------------------------------------------------------------------
#	---- --------------------------------------------------------------------------


#	---- --------------------------------------------------------------------------
#	---- aggiunta progetti alla soluzione
Write-Host "4.0) Associazione Progetti alla Soluzione"

	#	---- --------------------------------------------------------------------------
	Write-Host "4.1) Aggiunta Progetto Ordini.Api alla soluzione"
	dotnet sln add "Ordini.Api/Ordini.Api.csproj"
	#	---- --------------------------------------------------------------------------


	#	---- --------------------------------------------------------------------------
	Write-Host "4.2) Aggiunta Progetto Notifiche.Processor alla soluzione"
	dotnet sln add "Notifiche.Processor/Notifiche.Processor.csproj"
	#	---- --------------------------------------------------------------------------


	#	---- --------------------------------------------------------------------------
	Write-Host "4.3) Aggiunta Progetto Ordini.Processor alla soluzione"
	dotnet sln add "Ordini.Processor/Ordini.Processor.csproj"
	#	---- --------------------------------------------------------------------------


	#	---- --------------------------------------------------------------------------
	Write-Host "4.4) Aggiunta Progetto Pagamenti.Processor alla soluzione"
	dotnet sln add "Pagamenti.Processor/Pagamenti.Processor.csproj"
	#	---- --------------------------------------------------------------------------


	#	---- --------------------------------------------------------------------------
	Write-Host "4.5) Aggiunta Progetto Inventario.Processor alla soluzione"
	dotnet sln add "Inventario.Processor/Inventario.Processor.csproj"
	#	---- --------------------------------------------------------------------------


	#	---- --------------------------------------------------------------------------
	Write-Host "4.6) Aggiunta Progetto Libreria Ordini.Contracts alla soluzione"
	dotnet sln add "Ordini.Contracts/Ordini.Contracts.csproj"
	#	---- --------------------------------------------------------------------------
	
	
	#	---- --------------------------------------------------------------------------
	Write-Host "4.7) Aggiunta Progetto Libreria Ordini.ApplicationAPI.Models alla soluzione"
	dotnet sln add "Ordini.ApplicationAPI.Models/Ordini.ApplicationAPI.Models.csproj"
	#	---- --------------------------------------------------------------------------
	
	
	#	---- --------------------------------------------------------------------------
	#Write-Host "4.8) Aggiunta Progetto Libreria Ordini.LySystem.Models.Repositories alla soluzione"
	#dotnet sln add "Ordini.LySystem.Models.Repositories/Ordini.LySystem.Models.Repositories.csproj"
	#	---- --------------------------------------------------------------------------


#	---- --------------------------------------------------------------------------
#	---- --------------------------------------------------------------------------



#	---- --------------------------------------------------------------------------
#	---- Fine script
Write-Host "Fine Script"
Write-Host "Folder: ${Get-Location}"

Write-Host "Folder: $CartellaRoot"