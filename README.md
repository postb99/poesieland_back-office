![Unit Tests](https://github.com/postb99/poesieland_back-office/actions/workflows/dotnet.yml/badge.svg)

# Poesieland's back-office toolbox

## Why
I needed a management tool to perform operations on my French and English poems that you can read at https://poesieland.github.io/.

Historically, data has been stored privately in a single XML file.

Site content is stored as Markdown files. Conversion between storage (XML) and content (Markdown) — both export and import — is implemented in C#, my favorite programming language.

This also helps perform statistical computations using an in-memory XML document. With more than a thousand poems, memory is not an issue for now.

## What

### Content file generation

- Export a single poem from XML storage to a Markdown file with TOML front matter metadata.
- Same operation for a Season (a logical group of 50 poems).
- Generate a new Season main page (index) from XML storage using a template.

### Import to XML storage

- Import a specific poem by its ID (IDs end with the Season number). Metadata is read from the Markdown file in YAML or TOML format (YAML is the default used by the FrontMatter CMS editor).
- Same operation for a Season (a logical group of 50 poems).
- Season metadata can also be imported to allow easy editing while filling the season page.
- Import all English poems because they are included in some statistics. They're stored in a separate XML file.

### Generation of data files for use by Chart.js

- Overall statistics (bar, radar, pie) about poem lengths, verse lengths (metric), intervals between poems, and poems per day across the year timeline.
- Same statistics limited to a subset: a Season, a CMS category (a stored poem object subcategory), or a CMS tag (a stored poem object category).
- Distribution of a subset of poems across a Season.
- Line chart, rather than bar chart, for metrics over Seasons.
- More advanced charts (e.g., bubble charts) to show relationships between variables, such as poem length/metric, associated categories, or category/metric associations.

### Content quality check

Because CMS editor gives indications (fields descriptions) but a miss or typo can happen.

- Missing required tags (e.g., year, metric name).
- Missing required information when a metric is variable.
- Ensures the weight field of a new poem is encoded correctly (index plus one within its Season).
- Ensures a Season contains exactly 50 poems when it is considered complete.
- Checks poems that should be listed on special pages based on tags or characteristics.

### Helper functions

- Output a Season's duration (used on a specific page).
- Report reused titles — titles should generally be unique, but reuse is allowed after review and recorded in a control file. When a title is updated, the poem ID remains unchanged.

## How

Go to `src/Toolbox` and run `dotnet run`.

A menu appears in the command line. The most-used option is importing a single poem: choose 300, then 310, then enter the poem ID.

After importing a poem into the XML storage, the relevant data files for Chart.js are generated (not all files are regenerated). Specific Markdown files are also generated (e.g., total poem count), keeping site data up to date and easy to manage.

## External resources

[Chart.js, HTML5 open source charts](https://www.chartjs.org/)

[FrontMatter, CMS for markdown content](https://frontmatter.codes/docs/markdown)
