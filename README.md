# Poesieland's back-office toolbox

## Why
I needed a management tool to perform some operations with my French and also some English poems that you can read at https://poesieland.github.io/.

Since the beginning, data is privately stored in a single XML file.

The site content uses markdown files. So the storage to content initial operation and subsequent import from content to storage is coded using my favorite language, C#.

This also helps performing statistic computations, with a big thousand of poems it's not a memory hassle.

## What

### Content file generation

- For a single poem, from XML storage to markdown file. This produces metadata in TOML format markdown file.
- Same operation for a Season (a logical group of 50 poems).
- For a new Season, from XML storage to a markdown file, using a template for every Season main page (index).

### Import to XML storage

- For a given poem, giving its ID (that ends with Season's number). This reads metadata in YAML format from markdown file (default format used by FrontMatter CMS editor).
- Same operation for a Season (a logical group of 50 poems) or English poems (in a second XML storage file).

### Generation of data files for use by Chart.js

- Overall statistics about poems' length, poems' verses' length, poems' interval, poem by day across whole time.
- Same statistics but limited to a subset that is a Season, a CMS category (technically a subcategory), a CMS tag (technically a category or another metadata special value).

### Content quality check

- Especially about some metadata I can easily typo in CMS editor.
- To ensure Season has 50 poems when complete.

## How

Go to `src/Toolbox` and type `dotnet run`.

A menu displays in command line. The most used option is to import a single poem (type 300 then 310 then type poem ID).

Once the poem is imported to XML storage, relevant data files are generated for Chart.js, so not all data files.
There are also specific markdown files generated containing things like total poem count so that it's always up-to-date on site and easy to manage.

## External resources

[Chart.js, HTML5 open source charts](https://www.chartjs.org/)

[FrontMatter, CMS for markdown content](https://frontmatter.codes/docs/markdown)
