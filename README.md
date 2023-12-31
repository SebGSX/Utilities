# Utilities
A set of utilities. Currently, the project contains a listener thread implementation underpinned by a queue and low lock solution to ensure rapid, ordered processing of requests.

# Prompt Engineering
The project's primary purpose is to attend to a highly complex problem such that commensurately complex code is warranted. The nature of the problem is arbitrary, I happened to have needed a listener thread for other projects and thus used that problem for my needs here. The complex code is then used to refine the prompt script that configures ChatGPT such that the AI serves as a principal developer. In that role, the AI principal developer, whom I've named Tom, can pull request review code submitted. As the code complexity increased with development, so too did the complexity of the prompt to create Tom. The prompt is self-explanatory and can be found here: [Pull Request Review Prompt](https://github.com/SebGSX/Utilities/blob/main/prompt-engineering/pull-request-review.md). For more information on prompt engineering, see: [Prompt Engineering Guide](https://www.promptingguide.ai/).
