![Unit Tests](https://github.com/postb99/poesieland_back-office/actions/workflows/dotnet.yml/badge.svg)

# Poesieland's back-office toolbox

## Why
I needed a management tool to perform some operations with my French and also some English poems that you can read at https://poesieland.github.io/.

Since the beginning, data is privately stored in a single XML file.

The site content uses markdown files. So the storage to content initial operation and subsequent import from content to storage is coded using my favorite language, C#.

This also helps performing statistic computations using XML memory-loaded document, with more than a thousand of poems it's not a memory hassle, at least it works for now.

## What

### Content file generation

- For a single poem, from XML storage to markdown file. This produces metadata in TOML format markdown file.
- Same operation for a Season (a logical group of 50 poems).
- For a new Season main page, from XML storage to a markdown file, using a template for every Season main page (index).

### Import to XML storage

- For a given poem, giving its ID (that ends with Season's number). This reads metadata in YAML or TOML format from markdown file (YAML being default format used by FrontMatter CMS editor).
- Same operation for a Season (a logical group of 50 poems) or English poems (in a second XML storage file).
- A Season's metadata can be imported too, so that the markdown file is easily edited during its filling.
- All English poems because they're taken into account for some statistics. They have a separate XML storage file.

### Generation of data files for use by Chart.js

- Overall statistics (charts of types bar, radar, pie) about poems' length, poems' verse length (aka metric), poems' interval, poem by day across whole time.
- Same statistics but limited to a subset that is a Season, a CMS category (technically a subcategory), a CMS tag (technically a category or another metadata special value).
- Repartition of a given subset of poems across a Season.
- A line chart rather than a bar chart, for poem metric over Seasons.
- More evolved charts (of type bubble) that allow to display relationship between two variables, typically poem length by metric, associated categories, category/metric association.

### Content quality check

- Especially about some metadata I can easily typo or forget in CMS editor despite fields descriptions:
  - Miss of correctly filling required tags such as year, metric name
  - Miss of filling required information when metric is variable
  - 
- To ensure weight field of a new poem is encoded with right value corresponding to its index minus one in its Season.
- To ensure Season has exactly 50 poems when I think they're complete.
- For now, for poems with a given tag or characteristic being listed on special pages.

### Helper functions

- Output Seasons duration because this value is put on a specific page.
- Output reused titles because it's better to have different titles for every poem, but some reuses are allowed after careful review and stored in a control text file. When a title is updated, the poem ID remains the same.

## How

Go to `src/Toolbox` and type `dotnet run`.

A menu displays in command line. The most used option is to import a single poem (type 300 then 310 then type poem ID).

Once the poem is imported to XML storage, relevant data files are generated for Chart.js, so not all data files.
There are also specific markdown files generated containing things like total poem count so that it's always up-to-date on site and easy to manage.

## External resources

[Chart.js, HTML5 open source charts](https://www.chartjs.org/)

[FrontMatter, CMS for markdown content](https://frontmatter.codes/docs/markdown)
