# Spec-Driven Development
An experiment in Spec-Driven Development using VS Code in Agent Mode (with Claude Sonnet 4).
This is the repo used in [the corresponding YouTube video](https://www.youtube.com/watch?v=ex-HBo5t7IY).



Prompts:

1. Meeting all requirements in the copilot-instructions, generate the Base Application as defined in the Specifications/roadmap.md file. For now, ignore all Increments in this roadmap file.

Verify that all requirements are met. 

2. Now implement Increment 1 as defined in file roadmap.md.

3. Now implement Increment 2 as defined in file roadmap.md.

4. Now implement Increment 3 as defined in file roadmap.md.


## How to reproduce the generation of code?
Make sure you have VS Code with the *Github Copilot Chat* extension.

1. git clone https://github.com/mschluper/SpecDrivenDevelopment
2. cd SpecDrivenDevelopment
3. rm -rf src
4. rm -rf tests
5. Open *Copilot Chat* and give it the first prompt
6. See the magic happen (with a few confirmation clicks)
