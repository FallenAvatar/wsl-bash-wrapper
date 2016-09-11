# BashWrapper
A Wrapper for Bash through WSL that lets you interact with Bash normally from windows

# Limitations
1. You can not "interact" with the Bash process or any child processes it might spawn. You can ONLY read its **standard** output.
1. You may not be able to pipe a command correctly as the workaround uses piping internally

# Notes
1. The exit code from Bash is preserved and forwarded
1. All arguments passed to the wrapper are forwarded to Bash
1. Bash is started with a current working directory set to the Unix-style WSL version of the current working directory of the wrapper
  1. For instance, C:\Windows\System32 will be set as /mnt/c/Windows/System32/