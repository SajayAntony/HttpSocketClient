pushd ..\
dnvm use default -r coreclr 
for ($i=1;$i -lt 11; $i++) {
    $endpoint = "http://127.0.0."+ $i + ":5000/"
    $arguments = "run " + $endpoint
	start-process -FilePath dnx.exe -ArgumentList $arguments
}
popd 
