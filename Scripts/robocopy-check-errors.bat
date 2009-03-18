rem Check exit code and report success or any errors that occurred.
if errorlevel 16 echo ***FATAL ERROR*** & goto error
if errorlevel 8 echo **FAILED COPIES** & goto error
if errorlevel 4 echo *MISMATCHES* & goto error
if errorlevel 2 echo EXTRA FILES & goto error
if errorlevel 1 echo -Copy successful- & goto end
if errorlevel 0 echo -No change- & goto end
:end
exit /b 0
:error
exit /b 1
