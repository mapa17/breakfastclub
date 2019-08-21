#!/bin/bash

# Stop on any error
set -e

## Settings 
declare -a CLASS_CONFIGS=("ADHD-None" "ADHD-Low" "ADHD-Medium" "ADHD-High" "ADHD-VeryHigh" "ADHD-Medium-Ambitious" "ADHD-None-Ambitious" "Random")
#declare -a CLASS_CONFIGS=("ClassroomConfig-ADHD-None" "ClassroomConfig-ADHD-Medium")
OUTPUT_FOLDERS=()

CLASS_CONFIG_FOLDER="./classconfigs"
#SIMULATION_CONFIG="../Ressources/SimulationConfigs/SimulationConfigFile.json"
SIMULATION_CONFIG="./SimulationConfigFile.json"
# OUTFOLDER="./ADHD_Study"
OUTFOLDER=$1
NINSTANCES=5
SEED=424242

## Generate pictures
for CC in "${CLASS_CONFIGS[@]}"
do
    echo "Running simulation model ${CC} ..."
    python ../analysis/experiment.py ${CLASS_CONFIG_FOLDER}/${CC}.json ${OUTFOLDER}/${CC} --simulation-config-file ${SIMULATION_CONFIG} --seed ${SEED} --nInstances ${NINSTANCES} --headless --skip-agent-plots

    OUTPUT_FOLDERS+=(${OUTFOLDER}/${CC})
done

echo "Generating study results for " ${OUTPUT_FOLDERS[@]}
python ../analysis/study.py ${OUTFOLDER} ${OUTPUT_FOLDERS[@]}