# BashWrapper
A Wrapper for Bash through WSL that lets you interact with Bash normally from windows

# Limitations
1. You can not "interact" with the Bash process or any child processes it might spawn. You can ONLY read its **standard** output.
1. You may not be able to pipe a command correctly as the workaround uses piping internally

# Notes
The exit code from Bash is preserved and forwarded