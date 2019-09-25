# Working Directory
This folder is used to run experiments and studies. It contains a shellscript for convenience.

## Examples
Running a single experiment
```bash
python ../analysis/experiment.py classconfigs/ADHD-VeryHigh-Ambitious.json --simulation-config-file SimulationConfigFile.json --seed 11111 --headless PCA-test-ADHDVH
```

Run the complete study, storing to results to the folder 2019-09-18
```bash
./runStudy.sh 2019-09-18
```

Performing a post study analysis of folder results in folder 2019-09-18 and storing them to 2019-09-18-analysis
```bash
python ../analysis/analysis.py 2019-09-18/ 2019-09-18-analysis
```